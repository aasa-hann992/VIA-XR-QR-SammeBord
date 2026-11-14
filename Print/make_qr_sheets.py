# Laver de printbare QR-ark. Kræver: pip install reportlab   Kør: python make_qr_sheets.py
# Tilføj jeres egne ark i listen "sheets" (navn, indhold, beskrivelse). Indholdet XOR-kodes med KEY,
# præcis som SecretCode.Encode i Unity og QR-generator-siden (docs/index.html).
import base64
KEY = "VIA-XR"
def encode(content, key=KEY):
    b = bytearray(("XRV|" + content).encode("utf-8")); k = key.encode("utf-8")
    for i in range(len(b)): b[i] ^= k[i % len(k)]
    return "XRV1:" + base64.b64encode(bytes(b)).decode()
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.pdfgen import canvas
from reportlab.graphics.barcode.qr import QrCodeWidget
from reportlab.graphics.shapes import Drawing
from reportlab.graphics import renderPDF

W,H = A4
QR = 120*mm
CX, CY = W/2, 170*mm           # QR-kodens midte
CROSSES = [(-70*mm, -85*mm), (70*mm, -85*mm)]

sheets = [
 ("A","scene=tower;seed=1;h=8","Tårn (8 klodser)"),
 ("B","scene=tower;seed=2;h=12","Tårn (12 klodser)"),
 ("C","scene=orbit;seed=3","Planeter (styret af uret)"),
 ("D","scene=tower;seed=4;h=10","Tårn (10 klodser)"),
 ("E","scene=orbit;seed=5","Planeter (styret af uret)"),
 ("F","scene=tower;seed=6;h=15","Tårn (15 klodser)"),
 ("P","scene=physics;seed=7;h=12","Fysik-demoen: samme tårn, forskelligt fald"),
 ("L","id=L;scene=tower;seed=9;h=12","Udvidelse A - VENSTRE kode"),
 ("R","id=R","Udvidelse A - HØJRE kode"),
]

def cross(c,x,y,s=8*mm):
    c.setLineWidth(0.6); c.line(x-s,y,x+s,y); c.line(x,y-s,x,y+s)
    c.circle(x,y,2.5*mm)

c = canvas.Canvas("QR_ark.pdf", pagesize=A4)
c.setTitle("XR Viborg - QR-ark til tracking-øvelsen")
for name,content,desc in sheets:
    payload = encode(content)
    c.setFont("Helvetica-Bold",22); c.drawString(20*mm,H-22*mm,f"Ark {name}")
    c.setFont("Helvetica",12); c.drawString(45*mm,H-22*mm,desc)
    c.setFont("Helvetica",8.5); c.setFillGray(0.35)
    c.drawString(20*mm,H-29*mm,"XR Viborg · Tracking-øvelse · Print i 100 % / faktisk størrelse (ikke 'tilpas til side')")
    c.setFillGray(0)
    w = QrCodeWidget(payload, barLevel='M', barBorder=0)
    b = w.getBounds(); bw, bh = b[2]-b[0], b[3]-b[1]
    d = Drawing(QR, QR, transform=[QR/bw,0,0,QR/bh,0,0]); d.add(w)
    renderPDF.draw(d, c, CX-QR/2, CY-QR/2)
    # top-markering
    c.setFont("Helvetica-Bold",10)
    c.drawCentredString(CX, CY+QR/2+9*mm, "KODENS TOP")
    p = c.beginPath(); p.moveTo(CX-4*mm,CY+QR/2+3*mm); p.lineTo(CX+4*mm,CY+QR/2+3*mm); p.lineTo(CX,CY+QR/2+7.5*mm); p.close()
    c.drawPath(p, fill=1, stroke=0)
    # kryds
    for i,(dx,dy) in enumerate(CROSSES):
        cross(c,CX+dx,CY+dy)
        c.setFont("Helvetica",8)
        lbl = "VENSTRE kryds" if i==0 else "HØJRE kryds"
        c.drawCentredString(CX+dx, CY+dy-12*mm, lbl)
        c.drawCentredString(CX+dx, CY+dy-16*mm, f"x = {dx/mm:+.0f} mm, z = {dy/mm:+.0f} mm")
    c.setFont("Helvetica",8); c.drawCentredString(CX, CY+CROSSES[0][1]+2*mm, "-- de lilla kugler skal sidde præcis på krydsene --")
    # payload + målestok
    c.setFont("Courier",9); c.drawCentredString(CX, 44*mm, f"indhold:  {content}")
    c.setFont("Courier",7); c.drawCentredString(CX, 39*mm, f"QR-tekst (nøgle {KEY}):  {payload}")
    c.setLineWidth(1); y=25*mm; x0=CX-50*mm
    c.line(x0,y,x0+100*mm,y)
    for k in range(11):
        t = 3*mm if k%5==0 else 1.5*mm
        c.line(x0+k*10*mm,y,x0+k*10*mm,y+t)
    c.setFont("Helvetica",8); c.drawCentredString(CX, y-5*mm, "kontrol: denne streg skal være præcis 100 mm · QR-kodens sorte felt er 120 mm")
    c.drawCentredString(CX, 12*mm, "Læg arket fladt på bordet, stå ved den nederste kant. Origin = QR-kodens midte, +Z = mod kodens top.")
    c.showPage()
c.save()
print("ok")
