"""
generate_endpoint_plan_docx.py - builds docs/API_Endpoint_Plan.docx (and a PDF
export) from tools/endpoint_plan_source.md.

The layout follows the same house style as the PROG7314 Planning & Design
document: Aptos on A4, running header, numbered section headings ruled in
Office green accent 6, green-shaded table headers, and a Harvard reference list.

Section 3 is set landscape so the six required endpoint columns stay legible;
the rest of the document is portrait.

Usage:  python tools/generate_endpoint_plan_docx.py
Needs:  python-docx        (pip install python-docx)
        Microsoft Word     (optional - used only to export the PDF)
"""

import re
import subprocess
import sys
from pathlib import Path

from docx import Document
from docx.enum.section import WD_ORIENT, WD_SECTION
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_TAB_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Emu, Pt, RGBColor, Twips

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "tools" / "endpoint_plan_source.md"
OUTPUT = ROOT / "docs" / "API_Endpoint_Plan.docx"
PDF_OUT = ROOT / "docs" / "API_Endpoint_Plan.pdf"

# Office "Green, Accent 6" family, sampled from the reference document
GREEN_RULE = "70AD47"          # heading rule
GREEN_DARK = RGBColor(0x53, 0x81, 0x35)   # subsection headings
GREEN_HEAD = "C5E0B3"          # table header shading
GREEN_META = "E2EFD9"          # title-page metadata block
INK = RGBColor(0x1A, 0x1A, 0x1A)
CODE_INK = RGBColor(0xA0, 0x35, 0x28)

BODY_FONT, CODE_FONT = "Aptos", "Consolas"
MARGIN = Cm(2.5)

# table widths in twips (1 pt = 20 twips)
W_LAND = [850, 2900, 3300, 1450, 2700, 2800]     # 14000 = 700pt usable landscape
W_PORT_TOTAL = 9060                              # 453pt usable portrait


# ---------------------------------------------------------------- low level
def shade(cell, hex_fill):
    el = OxmlElement("w:shd")
    el.set(qn("w:val"), "clear")
    el.set(qn("w:fill"), hex_fill)
    cell._tc.get_or_add_tcPr().append(el)


def set_borders(table, colour="000000", size=4):
    tbl_pr = table._tbl.tblPr
    borders = OxmlElement("w:tblBorders")
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        e = OxmlElement(f"w:{edge}")
        e.set(qn("w:val"), "single")
        e.set(qn("w:sz"), str(size))
        e.set(qn("w:color"), colour)
        borders.append(e)
    tbl_pr.append(borders)


def no_borders(table):
    tbl_pr = table._tbl.tblPr
    borders = OxmlElement("w:tblBorders")
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        e = OxmlElement(f"w:{edge}")
        e.set(qn("w:val"), "none")
        borders.append(e)
    tbl_pr.append(borders)


def bottom_rule(par, colour=GREEN_RULE, size=12):
    pbdr = OxmlElement("w:pBdr")
    bottom = OxmlElement("w:bottom")
    bottom.set(qn("w:val"), "single")
    bottom.set(qn("w:sz"), str(size))
    bottom.set(qn("w:space"), "2")
    bottom.set(qn("w:color"), colour)
    pbdr.append(bottom)
    par._p.get_or_add_pPr().append(pbdr)


def hanging_indent(par, left=Cm(1.0)):
    pf = par.paragraph_format
    pf.left_indent = left
    pf.first_line_indent = -left


def repeat_header(row):
    tr_pr = row._tr.get_or_add_trPr()
    el = OxmlElement("w:tblHeader")
    el.set(qn("w:val"), "true")
    tr_pr.append(el)


def add_hyperlink(par, url, text):
    part = par.part
    r_id = part.relate_to(
        url,
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink",
        is_external=True)
    link = OxmlElement("w:hyperlink")
    link.set(qn("r:id"), r_id)
    run = OxmlElement("w:r")
    rPr = OxmlElement("w:rPr")
    for tag, val in (("w:color", "0563C1"), ("w:u", "single")):
        e = OxmlElement(tag)
        e.set(qn("w:val" if tag == "w:u" else "w:val"), "single" if tag == "w:u" else val)
        rPr.append(e)
    fonts = OxmlElement("w:rFonts")
    fonts.set(qn("w:ascii"), BODY_FONT)
    fonts.set(qn("w:hAnsi"), BODY_FONT)
    rPr.append(fonts)
    sz = OxmlElement("w:sz")
    sz.set(qn("w:val"), "21")
    rPr.append(sz)
    run.append(rPr)
    t = OxmlElement("w:t")
    t.text = text
    run.append(t)
    link.append(run)
    par._p.append(link)


# ------------------------------------------------------------ inline runs
TOKEN = re.compile(r"(`[^`]+`|\*\*[^*]+\*\*|\*[^*]+\*|https?://\S+)")


def write_runs(par, text, size=10.5, base_bold=False):
    """Render markdown inline formatting into a paragraph."""
    for tok in TOKEN.split(text):
        if not tok:
            continue
        if tok.startswith("`") and tok.endswith("`"):
            r = par.add_run(tok[1:-1])
            r.font.name, r.font.size, r.font.color.rgb = CODE_FONT, Pt(size - 1.5), CODE_INK
        elif tok.startswith("**") and tok.endswith("**"):
            r = par.add_run(tok[2:-2])
            r.bold = True
            r.font.name, r.font.size, r.font.color.rgb = BODY_FONT, Pt(size), INK
        elif tok.startswith("*") and tok.endswith("*") and len(tok) > 2:
            r = par.add_run(tok[1:-1])
            r.italic = True
            r.font.name, r.font.size, r.font.color.rgb = BODY_FONT, Pt(size), INK
        elif tok.startswith("http"):
            add_hyperlink(par, tok.rstrip(".,"), tok.rstrip(".,"))
            if tok[-1] in ".,":
                par.add_run(tok[-1])
        else:
            r = par.add_run(tok)
            r.bold = base_bold
            r.font.name, r.font.size, r.font.color.rgb = BODY_FONT, Pt(size), INK


# ------------------------------------------------------------------ parser
def split_row(line):
    return [c.strip() for c in line.strip().strip("|").split("|")]


def is_divider(line):
    return bool(re.fullmatch(r"\|[\s:|-]+\|", line.strip()))


def port_widths(ncols, header):
    """Proportional widths for a portrait table, summing to the text width."""
    if ncols == 2:
        return [2400, W_PORT_TOTAL - 2400]
    if ncols == 3 and header[0].lower().startswith("code"):
        return [2100, 2700, W_PORT_TOTAL - 4800]
    if ncols == 3:
        return [3600, 1800, W_PORT_TOTAL - 5400]
    w = W_PORT_TOTAL // ncols
    return [w] * (ncols - 1) + [W_PORT_TOTAL - w * (ncols - 1)]


def build():
    doc = Document()

    st = doc.styles["Normal"]
    st.font.name = BODY_FONT
    st.font.size = Pt(10.5)
    st.font.color.rgb = INK
    st.paragraph_format.space_after = Pt(8)

    sec = doc.sections[0]
    sec.page_width, sec.page_height = Cm(21.0), Cm(29.7)
    sec.left_margin = sec.right_margin = MARGIN
    sec.top_margin = sec.bottom_margin = Cm(2.2)

    def style_header(section):
        """Two borderless cells rather than a tab: Word's built-in Header style
        carries its own centre tab, which swallows a tabbed right-align."""
        hdr = section.header
        hdr.paragraphs[0].text = ""
        hdr.paragraphs[0].paragraph_format.space_after = Pt(0)
        width = Emu(section.page_width - section.left_margin - section.right_margin)
        half = int(width.twips / 2)
        t = hdr.add_table(rows=1, cols=2, width=width)
        no_borders(t)
        for ci, (txt, align) in enumerate((("PROG6212", WD_ALIGN_PARAGRAPH.LEFT),
                                           ("POE PART 1", WD_ALIGN_PARAGRAPH.RIGHT))):
            c = t.cell(0, ci)
            c.width = Twips(half)
            par = c.paragraphs[0]
            par.alignment = align
            par.paragraph_format.space_after = Pt(0)
            r = par.add_run(txt)
            r.bold = True
            r.font.name, r.font.size, r.font.color.rgb = BODY_FONT, Pt(9.5), INK

    style_header(sec)

    lines = SOURCE.read_text(encoding="utf-8").splitlines()
    i, in_refs, contents = 0, False, []
    for ln in lines:
        if ln.startswith("## ") and re.match(r"## \d+\.", ln):
            contents.append(ln[3:].strip())

    pending = []

    def flush():
        if not pending:
            return
        text = " ".join(pending)
        pending.clear()
        p = doc.add_paragraph()
        if in_refs:
            hanging_indent(p)
            p.paragraph_format.space_after = Pt(6)
        write_runs(p, text)

    while i < len(lines):
        line = lines[i].rstrip()

        # ---- directives -------------------------------------------------
        if line.startswith("<!-- meta"):
            flush()
            rows = []
            i += 1
            while not lines[i].startswith("-->"):
                if "|" in lines[i]:
                    rows.append(lines[i].split("|", 1))
                i += 1
            i += 1
            t = doc.add_table(rows=len(rows), cols=2)
            t.alignment = WD_TABLE_ALIGNMENT.CENTER
            no_borders(t)
            for ri, (k, v) in enumerate(rows):
                for ci, (txt, bold) in enumerate(((k, False), (v, True))):
                    c = t.cell(ri, ci)
                    c.width = Twips(2400 if ci == 0 else 4200)
                    shade(c, GREEN_META)
                    par = c.paragraphs[0]
                    par.paragraph_format.space_after = Pt(3)
                    r = par.add_run(txt)
                    r.bold = bold
                    r.font.name, r.font.size, r.font.color.rgb = BODY_FONT, Pt(11), INK
            doc.add_page_break()
            continue

        if line.startswith("<!-- contents"):
            flush()
            h = doc.add_paragraph()
            hr = h.add_run("CONTENTS")
            hr.bold = True
            hr.font.name, hr.font.size, hr.font.color.rgb = BODY_FONT, Pt(15), INK
            bottom_rule(h)
            for entry in contents:
                p = doc.add_paragraph()
                p.paragraph_format.space_after = Pt(2)
                hanging_indent(p, Cm(0.9))
                write_runs(p, entry)
            i += 1
            continue

        if line.startswith("<!-- landscape"):
            flush()
            s = doc.add_section(WD_SECTION.NEW_PAGE)
            s.orientation = WD_ORIENT.LANDSCAPE
            s.page_width, s.page_height = Cm(29.7), Cm(21.0)
            s.left_margin = s.right_margin = MARGIN
            s.top_margin = s.bottom_margin = Cm(2.0)
            s.header.is_linked_to_previous = False
            style_header(s)
            i += 1
            continue

        if line.startswith("<!-- portrait"):
            flush()
            s = doc.add_section(WD_SECTION.NEW_PAGE)
            s.orientation = WD_ORIENT.PORTRAIT
            s.page_width, s.page_height = Cm(21.0), Cm(29.7)
            s.left_margin = s.right_margin = MARGIN
            s.top_margin = s.bottom_margin = Cm(2.2)
            s.header.is_linked_to_previous = False
            style_header(s)
            i += 1
            continue

        # ---- tables -----------------------------------------------------
        if line.startswith("|") and i + 1 < len(lines) and is_divider(lines[i + 1]):
            flush()
            header = split_row(line)
            i += 2
            rows = []
            while i < len(lines) and lines[i].strip().startswith("|"):
                rows.append(split_row(lines[i]))
                i += 1

            landscape = len(header) == 6
            widths = W_LAND if landscape else port_widths(len(header), header)
            fsize = 8.0 if landscape else 9.5

            t = doc.add_table(rows=1, cols=len(header))
            t.alignment = WD_TABLE_ALIGNMENT.CENTER
            t.autofit = False
            set_borders(t)
            repeat_header(t.rows[0])
            for ci, htxt in enumerate(header):
                c = t.cell(0, ci)
                c.width = Twips(widths[ci])
                shade(c, GREEN_HEAD)
                par = c.paragraphs[0]
                par.paragraph_format.space_after = Pt(1)
                r = par.add_run(htxt)
                r.bold = True
                r.font.name, r.font.size, r.font.color.rgb = BODY_FONT, Pt(fsize), INK
            for row in rows:
                cells = t.add_row().cells
                for ci, txt in enumerate(row[:len(header)]):
                    cells[ci].width = Twips(widths[ci])
                    par = cells[ci].paragraphs[0]
                    par.paragraph_format.space_after = Pt(1)
                    write_runs(par, txt, size=fsize, base_bold=(ci == 0 and landscape))
            doc.add_paragraph()
            continue

        # ---- headings ---------------------------------------------------
        if line.startswith("# "):
            flush()
            for _ in range(6):
                doc.add_paragraph()
            p = doc.add_paragraph()
            p.alignment = WD_ALIGN_PARAGRAPH.CENTER
            p.paragraph_format.space_after = Pt(2)
            r = p.add_run(line[2:].strip())
            r.bold = True
            r.font.name, r.font.size, r.font.color.rgb = BODY_FONT, Pt(36), INK
            i += 1
            continue

        if line.startswith("## "):
            flush()
            text = line[3:].strip()
            if i < 4:                                    # subtitle on the title page
                p = doc.add_paragraph()
                p.alignment = WD_ALIGN_PARAGRAPH.CENTER
                p.paragraph_format.space_after = Pt(18)
                r = p.add_run(text)
                r.font.name, r.font.size, r.font.color.rgb = BODY_FONT, Pt(12), INK
                i += 1
                continue
            in_refs = text.upper().endswith("REFERENCES")
            p = doc.add_paragraph()
            p.paragraph_format.space_before = Pt(14)
            p.paragraph_format.space_after = Pt(8)
            r = p.add_run(text.upper())
            r.bold = True
            r.font.name, r.font.size, r.font.color.rgb = BODY_FONT, Pt(15), INK
            bottom_rule(p)
            i += 1
            continue

        if line.startswith("### "):
            flush()
            p = doc.add_paragraph()
            p.paragraph_format.space_before = Pt(10)
            p.paragraph_format.space_after = Pt(4)
            r = p.add_run(line[4:].strip())
            r.bold = True
            r.font.name, r.font.size, r.font.color.rgb = BODY_FONT, Pt(12), GREEN_DARK
            i += 1
            continue

        if line.strip() in ("---", ""):
            flush()
            i += 1
            continue

        pending.append(line.strip())
        i += 1

    flush()
    doc.save(OUTPUT)
    print(f"wrote {OUTPUT}  ({OUTPUT.stat().st_size // 1024} KB)")


def set_modern_compat(path):
    """python-docx ships a Word 2010 template, so Word opens the result in
    Compatibility Mode. Raising compatibilityMode to 15 clears that banner."""
    import shutil
    import zipfile

    tmp = path.with_suffix(".tmp.docx")
    with zipfile.ZipFile(path) as zin, zipfile.ZipFile(tmp, "w", zipfile.ZIP_DEFLATED) as zout:
        for item in zin.infolist():
            data = zin.read(item.filename)
            if item.filename == "word/settings.xml":
                data = data.decode("utf-8").replace(
                    '<w:compatSetting w:name="compatibilityMode" '
                    'w:uri="http://schemas.microsoft.com/office/word" w:val="14"/>',
                    '<w:compatSetting w:name="compatibilityMode" '
                    'w:uri="http://schemas.microsoft.com/office/word" w:val="15"/>'
                ).encode("utf-8")
            zout.writestr(item, data)
    shutil.move(tmp, path)


def export_pdf():
    """Export the .docx to PDF through Word so both deliverables match."""
    ps = (
        "$w = New-Object -ComObject Word.Application; $w.Visible = $false; "
        f"$d = $w.Documents.Open('{OUTPUT}', [ref]$false, [ref]$true); "
        f"$d.SaveAs([ref]'{PDF_OUT}', [ref]17); $d.Close([ref]0); $w.Quit()"
    )
    r = subprocess.run(["powershell", "-NoProfile", "-Command", ps],
                       capture_output=True, text=True)
    if PDF_OUT.exists():
        print(f"wrote {PDF_OUT}  ({PDF_OUT.stat().st_size // 1024} KB)")
    else:
        print("PDF export skipped (Word unavailable):", r.stderr.strip()[:200], file=sys.stderr)


if __name__ == "__main__":
    build()
    set_modern_compat(OUTPUT)
    export_pdf()
