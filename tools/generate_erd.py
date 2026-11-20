"""
generate_erd.py - builds the RaceDay ERD in draw.io style.

Outputs two files:
  docs/RaceDay_ERD.drawio   editable source, opens in draw.io / diagrams.net
  docs/RaceDay_ERD.png      the submitted image, with the diagram XML embedded
                            in a PNG text chunk so the image itself reopens in
                            draw.io the same way a .drawio.png export does

Styling follows the draw.io UML default: white fill, 1px black border, bold
centred title compartment, left aligned attribute rows, plain orthogonal
connectors with multiplicity labels.

Usage:  python tools/generate_erd.py
Needs:  Pillow  (pip install pillow)
"""

import urllib.parse
from pathlib import Path
from xml.sax.saxutils import escape

from PIL import Image, ImageDraw, ImageFont, PngImagePlugin

ROOT = Path(__file__).resolve().parent.parent
PNG_OUT = ROOT / "docs" / "RaceDay_ERD.png"
XML_OUT = ROOT / "docs" / "RaceDay_ERD.drawio"

# draw.io geometry, in diagram pixels
BOX_W = 260
HEADER_H = 26
ROW_H = 20
COL_X = [40, 410, 780, 1150]
ROW_Y = [40, 460]
CANVAS = (1450, 810)

SCALE = 3                      # supersample for a clean downscale
FONT_DIR = Path("C:/Windows/Fonts")
F_TITLE = ImageFont.truetype(str(FONT_DIR / "arialbd.ttf"), 12 * SCALE)
F_ATTR = ImageFont.truetype(str(FONT_DIR / "arial.ttf"), 11 * SCALE)
F_EDGE = ImageFont.truetype(str(FONT_DIR / "arial.ttf"), 10 * SCALE)
F_NOTE = ImageFont.truetype(str(FONT_DIR / "arial.ttf"), 11 * SCALE)

BLACK = (0, 0, 0)
WHITE = (255, 255, 255)

# --------------------------------------------------------------------------
# Model. Each attribute is (name, type, stereotype)
# --------------------------------------------------------------------------
ENTITIES = {
    "Roles": (0, 0, [
        ("RoleId", "int", "PK"),
        ("RoleName", "varchar(20)", "UQ"),
        ("Description", "varchar(150)", ""),
    ]),
    "Users": (1, 0, [
        ("UserId", "int", "PK"),
        ("RoleId", "int", "FK"),
        ("FirstName", "varchar(60)", ""),
        ("LastName", "varchar(60)", ""),
        ("Email", "varchar(150)", "UQ"),
        ("PasswordHash", "varchar(256)", ""),
        ("PhoneNumber", "varchar(20)", ""),
        ("DateOfBirth", "date", ""),
        ("Gender", "char(1)", ""),
        ("CreatedAt", "datetime2", ""),
        ("IsActive", "bit", ""),
    ]),
    "Events": (2, 0, [
        ("EventId", "int", "PK"),
        ("OrganiserId", "int", "FK"),
        ("EventName", "varchar(120)", ""),
        ("Description", "varchar(1000)", ""),
        ("EventDate", "date", ""),
        ("StartTime", "time", ""),
        ("Province", "varchar(40)", ""),
        ("City", "varchar(80)", ""),
        ("VenueName", "varchar(120)", ""),
        ("Latitude", "decimal(9,6)", ""),
        ("Longitude", "decimal(9,6)", ""),
        ("RegistrationOpens", "date", ""),
        ("RegistrationCloses", "date", ""),
        ("EventStatus", "varchar(20)", ""),
        ("CreatedAt", "datetime2", ""),
    ]),
    "WeatherSnapshots": (3, 0, [
        ("SnapshotId", "int", "PK"),
        ("EventId", "int", "FK"),
        ("CapturedAt", "datetime2", ""),
        ("TemperatureC", "decimal(4,1)", ""),
        ("FeelsLikeC", "decimal(4,1)", ""),
        ("Conditions", "varchar(60)", ""),
        ("WindSpeedKph", "decimal(5,1)", ""),
        ("HumidityPercent", "tinyint", ""),
        ("ChanceOfRainPct", "tinyint", ""),
        ("Source", "varchar(60)", ""),
    ]),
    "Results": (0, 1, [
        ("ResultId", "int", "PK"),
        ("EnrolmentId", "int", "FK, UQ"),
        ("CapturedByUserId", "int", "FK"),
        ("StartTime", "time", ""),
        ("FinishTime", "time", ""),
        ("ChipTimeSeconds", "int", ""),
        ("OverallPosition", "int", ""),
        ("GenderPosition", "int", ""),
        ("AgeGroupPosition", "int", ""),
        ("ResultStatus", "varchar(10)", ""),
        ("CapturedAt", "datetime2", ""),
    ]),
    "Enrolments": (1, 1, [
        ("EnrolmentId", "int", "PK"),
        ("ParticipantId", "int", "FK"),
        ("CategoryId", "int", "FK"),
        ("RaceNumber", "varchar(10)", ""),
        ("EnrolmentDate", "datetime2", ""),
        ("EnrolmentStatus", "varchar(20)", ""),
        ("AmountPaid", "decimal(10,2)", ""),
        ("PaymentReference", "varchar(50)", ""),
        ("TShirtSize", "varchar(5)", ""),
        ("EmergencyContactName", "varchar(100)", ""),
        ("EmergencyContactPhone", "varchar(20)", ""),
    ]),
    "EventCategories": (2, 1, [
        ("CategoryId", "int", "PK"),
        ("EventId", "int", "FK"),
        ("CategoryName", "varchar(80)", ""),
        ("Discipline", "varchar(10)", ""),
        ("DistanceKm", "decimal(6,2)", ""),
        ("EntryFee", "decimal(10,2)", ""),
        ("MaxParticipants", "int", ""),
        ("StartTime", "time", ""),
        ("MinimumAge", "int", ""),
    ]),
    "RouteWaypoints": (3, 1, [
        ("WaypointId", "int", "PK"),
        ("CategoryId", "int", "FK"),
        ("SequenceNumber", "int", ""),
        ("WaypointName", "varchar(80)", ""),
        ("WaypointType", "varchar(20)", ""),
        ("Latitude", "decimal(9,6)", ""),
        ("Longitude", "decimal(9,6)", ""),
        ("DistanceFromStartKm", "decimal(6,2)", ""),
    ]),
}


def attr_text(a):
    name, dtype, stereo = a
    s = f"- {name} : {dtype}"
    return f"{s} <<{stereo}>>" if stereo else s


def rect(name):
    cx, ry, attrs = ENTITIES[name]
    x, y = COL_X[cx], ROW_Y[ry]
    return x, y, x + BOX_W, y + HEADER_H + len(attrs) * ROW_H


# --------------------------------------------------------------------------
# Relationships: source, target, waypoint polyline, labels
# --------------------------------------------------------------------------
RELATIONSHIPS = [
    dict(src="Roles", dst="Users", pts=[(300, 89), (410, 89)],
         verb="classifies", s_mult="1", t_mult="0..*"),
    dict(src="Users", dst="Events", pts=[(670, 120), (780, 120)],
         verb="organises", s_mult="1", t_mult="0..*"),
    dict(src="Events", dst="WeatherSnapshots", pts=[(1040, 170), (1150, 170)],
         verb="forecast for", s_mult="1", t_mult="0..*"),
    dict(src="Events", dst="EventCategories", pts=[(910, 378), (910, 460)],
         verb="offers", s_mult="1", t_mult="1..*"),
    dict(src="EventCategories", dst="RouteWaypoints", pts=[(1040, 560), (1150, 560)],
         verb="routed by", s_mult="1", t_mult="0..*"),
    dict(src="EventCategories", dst="Enrolments", pts=[(780, 630), (670, 630)],
         verb="receives", s_mult="1", t_mult="0..*"),
    dict(src="Users", dst="Enrolments", pts=[(540, 298), (540, 460)],
         verb="places", s_mult="1", t_mult="0..*"),
    dict(src="Enrolments", dst="Results", pts=[(410, 690), (300, 690)],
         verb="scores", s_mult="1", t_mult="0..1"),
    dict(src="Users", dst="Results", pts=[(410, 240), (355, 240), (355, 520), (300, 520)],
         verb="captures", s_mult="1", t_mult="0..*"),
]

NOTE = ("Enrolments resolves the many-to-many between Users and EventCategories. "
        "A participant may enter many categories\nand a category may hold many participants. "
        "UNIQUE (ParticipantId, CategoryId) prevents a duplicate entry.")


# ==========================================================================
# PNG rendering
# ==========================================================================
def S(v):
    return int(round(v * SCALE))


def draw_png():
    img = Image.new("RGB", (S(CANVAS[0]), S(CANVAS[1])), WHITE)
    d = ImageDraw.Draw(img)
    lw = max(1, SCALE // 2)

    # edges first so the boxes sit on top of any overlap
    for rel in RELATIONSHIPS:
        pts = [(S(x), S(y)) for x, y in rel["pts"]]
        d.line(pts, fill=BLACK, width=lw)

    for name, (cx, ry, attrs) in ENTITIES.items():
        x0, y0, x1, y1 = rect(name)
        d.rectangle([S(x0), S(y0), S(x1), S(y1)], fill=WHITE, outline=BLACK, width=lw)
        # title compartment
        d.line([S(x0), S(y0 + HEADER_H), S(x1), S(y0 + HEADER_H)], fill=BLACK, width=lw)
        tw = d.textlength(name, font=F_TITLE)
        d.text((S(x0) + (S(BOX_W) - tw) / 2, S(y0 + 6)), name, font=F_TITLE, fill=BLACK)
        for i, a in enumerate(attrs):
            d.text((S(x0 + 5), S(y0 + HEADER_H + i * ROW_H + 4)),
                   attr_text(a), font=F_ATTR, fill=BLACK)

    # multiplicity + verb labels
    for rel in RELATIONSHIPS:
        pts = rel["pts"]
        _label(d, pts[0], pts[1], rel["s_mult"])
        _label(d, pts[-1], pts[-2], rel["t_mult"])
        mx, my = _midpoint(pts)
        horizontal = abs(pts[0][1] - pts[1][1]) < 1
        tw = d.textlength(rel["verb"], font=F_EDGE)
        if horizontal:
            cx, cy = S(mx) - tw / 2, S(my + 4)
        else:
            cx, cy = S(mx - 10) - tw, S(my) - S(6)
        d.rectangle([cx - S(3), cy - S(2), cx + tw + S(3), cy + S(13)], fill=WHITE)
        d.text((cx, cy), rel["verb"], font=F_EDGE, fill=BLACK)

    for i, line in enumerate(NOTE.split("\n")):
        d.text((S(40), S(752 + i * 16)), line, font=F_NOTE, fill=BLACK)

    img = img.resize(CANVAS, Image.LANCZOS)

    meta = PngImagePlugin.PngInfo()
    meta.add_text("mxfile", urllib.parse.quote(build_xml(), safe="~()*!.'"))
    img.save(PNG_OUT, "PNG", pnginfo=meta, optimize=True)
    return img


def _label(d, end, toward, text):
    """Multiplicity sits just off the box, above a horizontal line or right of
    a vertical one. The verb label uses the opposite side, so they never clash."""
    ex, ey = end
    tx, ty = toward
    dx, dy = tx - ex, ty - ey
    n = max(abs(dx), abs(dy)) or 1
    ux, uy = dx / n, dy / n
    px, py = ex + ux * 15, ey + uy * 15
    if abs(ux) > abs(uy):
        py -= 9
    else:
        px += 16
    tw = d.textlength(text, font=F_EDGE)
    d.text((S(px) - tw / 2, S(py) - S(6)), text, font=F_EDGE, fill=BLACK)


def _midpoint(pts):
    if len(pts) == 2:
        return ((pts[0][0] + pts[1][0]) / 2, (pts[0][1] + pts[1][1]) / 2)
    a, b = pts[len(pts) // 2 - 1], pts[len(pts) // 2]
    return ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)


# ==========================================================================
# draw.io XML
# ==========================================================================
BOX_STYLE = ("swimlane;fontStyle=1;align=center;verticalAlign=top;childLayout=stackLayout;"
             "horizontal=1;startSize=26;horizontalStack=0;resizeParent=1;resizeParentMax=0;"
             "html=1;fillColor=#ffffff;strokeColor=#000000;fontColor=#000000;fontSize=12;")
ROW_STYLE = ("text;strokeColor=none;fillColor=none;align=left;verticalAlign=middle;"
             "spacingLeft=4;spacingRight=4;overflow=hidden;rotatable=0;points=[[0,0.5],[1,0.5]];"
             "portConstraint=eastwest;whiteSpace=wrap;html=1;fontSize=11;fontColor=#000000;")
EDGE_STYLE = ("edgeStyle=orthogonalEdgeStyle;rounded=0;html=1;endArrow=none;startArrow=none;"
              "strokeColor=#000000;fontSize=10;fontColor=#000000;exitX={ex};exitY={ey};"
              "exitDx=0;exitDy=0;entryX={nx};entryY={ny};entryDx=0;entryDy=0;")
NOTE_STYLE = ("text;html=1;align=left;verticalAlign=top;strokeColor=none;fillColor=none;"
              "fontSize=11;fontColor=#000000;whiteSpace=wrap;")


def _anchor(name, point):
    """Turn an absolute waypoint into the exit/entry fraction on that box."""
    x0, y0, x1, y1 = rect(name)
    px, py = point
    if abs(px - x0) < 1:
        return 0, round((py - y0) / (y1 - y0), 4)
    if abs(px - x1) < 1:
        return 1, round((py - y0) / (y1 - y0), 4)
    if abs(py - y0) < 1:
        return round((px - x0) / (x1 - x0), 4), 0
    return round((px - x0) / (x1 - x0), 4), 1


def build_xml():
    cells, nid = [], 2
    ids = {}
    for name, (cx, ry, attrs) in ENTITIES.items():
        x0, y0, x1, y1 = rect(name)
        ids[name] = str(nid)
        cells.append(
            f'<mxCell id="{nid}" value="{escape(name)}" style="{BOX_STYLE}" vertex="1" parent="1">'
            f'<mxGeometry x="{x0}" y="{y0}" width="{BOX_W}" height="{y1 - y0}" as="geometry"/></mxCell>')
        parent = nid
        nid += 1
        for i, a in enumerate(attrs):
            cells.append(
                f'<mxCell id="{nid}" value="{escape(attr_text(a))}" style="{ROW_STYLE}" '
                f'vertex="1" parent="{parent}">'
                f'<mxGeometry y="{HEADER_H + i * ROW_H}" width="{BOX_W}" height="{ROW_H}" '
                f'as="geometry"/></mxCell>')
            nid += 1

    for rel in RELATIONSHIPS:
        ex, ey = _anchor(rel["src"], rel["pts"][0])
        nx, ny = _anchor(rel["dst"], rel["pts"][-1])
        style = EDGE_STYLE.format(ex=ex, ey=ey, nx=nx, ny=ny)
        eid = nid
        nid += 1
        mids = rel["pts"][1:-1]
        pts_xml = ""
        if mids:
            pts_xml = ("<Array as=\"points\">"
                       + "".join(f'<mxPoint x="{x}" y="{y}"/>' for x, y in mids)
                       + "</Array>")
        cells.append(
            f'<mxCell id="{eid}" value="{escape(rel["verb"])}" style="{style}" edge="1" '
            f'parent="1" source="{ids[rel["src"]]}" target="{ids[rel["dst"]]}">'
            f'<mxGeometry relative="1" as="geometry">{pts_xml}</mxGeometry></mxCell>')
        for frac, text in ((-1, rel["s_mult"]), (1, rel["t_mult"])):
            cells.append(
                f'<mxCell id="{nid}" value="{escape(text)}" '
                f'style="edgeLabel;html=1;align=center;verticalAlign=middle;resizable=0;'
                f'fontSize=10;fontColor=#000000;" vertex="1" connectable="0" parent="{eid}">'
                f'<mxGeometry x="{frac}" relative="1" as="geometry">'
                f'<mxPoint as="offset" y="-10"/></mxGeometry></mxCell>')
            nid += 1

    cells.append(
        f'<mxCell id="{nid}" value="{escape(NOTE.replace(chr(10), " "))}" style="{NOTE_STYLE}" '
        f'vertex="1" parent="1">'
        f'<mxGeometry x="40" y="745" width="1370" height="40" as="geometry"/></mxCell>')

    body = "".join(cells)
    return (
        '<mxfile host="app.diagrams.net" type="device">'
        '<diagram name="RaceDay ERD" id="raceday-erd">'
        '<mxGraphModel dx="1422" dy="800" grid="1" gridSize="10" guides="1" tooltips="1" '
        'connect="1" arrows="1" fold="1" page="1" pageScale="1" pageWidth="1169" pageHeight="826" '
        'math="0" shadow="0">'
        '<root><mxCell id="0"/><mxCell id="1" parent="0"/>'
        f'{body}'
        '</root></mxGraphModel></diagram></mxfile>'
    )


def main():
    xml = build_xml()
    XML_OUT.write_text(xml, encoding="utf-8")
    draw_png()
    print(f"wrote {XML_OUT.name}  ({XML_OUT.stat().st_size // 1024} KB)")
    print(f"wrote {PNG_OUT.name}  ({PNG_OUT.stat().st_size // 1024} KB)")


if __name__ == "__main__":
    main()
