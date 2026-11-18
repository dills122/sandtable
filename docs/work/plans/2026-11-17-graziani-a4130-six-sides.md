# Graziani fixed Axis setup: Map A, A4130 Soluch visual evidence

Source: `/private/tmp/sandtable-maps.pdf`, PDF page 1, printed Map A. PDF SHA-256: `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. This is a visual transcription of the supplied scan, not a movement or setup legality ruling.

## Reproducible crop

Render page 1 with `pdftoppm -f 1 -l 1 -scale-to 8000 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/graziani-a4130/map-a`; rendered page is 5037 x 8000 pixels. Crop rectangle `(4050, 2400, 5000, 3400)`, measured from the top left, right/bottom exclusive, with Pillow. Unrotated crop SHA-256: `f4a5b1a46b55a0f7076d18c05259f3146541bdfbff44fc19a84ecf22df39bf96`. Rendered whole-page PNG SHA-256: `cd2c0a6b5723b983391979c9f46b37dc8dc417d99f7f29ca8b57d866e4fe8158`. PNG hashes depend on rendering/encoding versions; PDF hash anchors source.

## Hex and sides

Directions below refer to the unrotated crop, not compass bearings. The printed `4130` is in a pale clear hex; the blue Soluch village dot and label are near its lower half. All six neighboring labels carry Map A prefix. The scan's black tracks converge over the blue railroad and village mark, making their exact junction hard to separate.

| Side in crop | Neighbor | Visible crossing or mark |
| --- | --- | --- |
| Upper left | A4229 | Blue railroad crosses sloping side toward Soluch. |
| Upper right | A4230 | Black dashed track crosses sloping side toward Soluch. |
| Right | A4131 | Black dashed track crosses side, continuing from Soluch toward A4131. |
| Lower right | A4030 | Black dashed track crosses sloping side southeast of Soluch in crop. |
| Lower left | A4029 | Black dashed track crosses sloping side southwest of Soluch in crop. |
| Left | A4129 | Black dashed track crosses side toward Soluch. |

The blue railroad, village dot, and black track strokes overlap near Soluch; a track also appears to branch there. The label, blue dot, and lines obscure small portions of the underlying print. No colored ridge, escarpment, slope, wadi, river, or border symbol is apparent on A4130's six sides in this crop. Treat this as a scan observation, not proof about rules or unobscured print.

Map A's printed terrain key identifies pale background as **Clear**, blue dot as **Village/Bir.**, solid blue line with crossbars as **Railroad**, black dashed line as **Track**, and paired solid black lines as **Road**. Soluch's black dashed marks match the track symbol, not the road symbol. Map A's key also distinguishes the blue dashed **Unfinished Railroad** symbol; the Soluch line appears solid between crossbars. Adjacent A4131 and A4230 contain other track segments; their existence does not by itself establish legal traversal.
