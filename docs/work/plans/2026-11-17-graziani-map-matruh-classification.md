# Matruh D3714: map symbol classification

**Finding:** The original Map D marks Mersa Matruh D3714 with the blue dot identified by the original terrain key as **Village/Bir.**, not the cluster of blue blocks identified as **Major City**. The nearby anchor is a separate port symbol. This supports classifying the map hex as Village/Bir for terrain purposes; it does not resolve Land §32.14's `off-map` wording, scenario setup exceptions, repair facility status, or a legal supply-unit capacity.

## Source and inspection

Visually inspected [original five-sheet map](https://spigames.net/PDFv10/CNA_Maps.pdf), PDF p. 4 Map D around Mersa Matruh and PDF p. 1 terrain key; verified SHA-256 `8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030`. Also inspected [Land rules](https://www.spigames.net/PDFv10/CNA_LandGameRules.pdf), PDF/printed p. 43 §32.14, verified SHA-256 `b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99`. The earlier [Fourth Indian edge check](2026-11-17-graziani-fourth-indian-edges-next.md) supplied crop coordinates and already called the mark a village dot; this is a targeted same-scan visual check, not blind or another edition. Hashes identify bytes, not redistribution rights.

```sh
pdftoppm -f 4 -l 4 -r 500 -x 3900 -y 6750 -W 1500 -H 1350 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/matruh-classification/d
pdftoppm -f 1 -l 1 -r 300 -x 2250 -y 300 -W 2100 -H 1800 -png -singlefile /private/tmp/sandtable-maps.pdf /private/tmp/matruh-classification/key
pdftoppm -f 43 -l 43 -r 200 -png -singlefile /private/tmp/sandtable-land-rules.pdf /private/tmp/matruh-classification/rules
```

PNG hashes in command order: `fdafec13776eb095d135bdabac0af3b1ff696975f907e9ce02942e90f492e191`, `8218f9ff8e4f6c9a49c0e631fe2a9b861ef82dd81dfb9107da4e759ccf155894`, `366c7dc72d595dc8592e877056aeab0448edc1e24858d2c3a7dd2142698b1a3e`. Renderer bytes may differ. Crops remain outside Git.

In the D crop, the printed `3714` is immediately left of the blue circular settlement mark, inside the same hex, near (425, 425). The double-line road crosses near the dot and partially crowds its edge, but the dot remains distinguishable. The port anchor lies north at the coast, near (470, 265), and matches the key's distinct port symbol. The key shows a solitary blue dot for Village/Bir and a group of blue building blocks for Major City. No major-city block cluster is visible in D3714 in this crop. The full name is partly cropped on the left, but the visible `...rsa Matruh`, printed coordinate, port and adjoining geography identify the location. This finding concerns the printed map mark; it is not a claim that every scenario or rule uses the same term consistently.

Section 32.14 states five supply units may be placed in a Major City hex and three in any other type of hex `off-map`, while Tripoli/Tunisia boxes have unlimited capacity. D3714's map symbol does not meet the map key's Major City symbol, so its five-unit clause does not follow merely from the map mark. The printed `off-map` grammar leaves its on-map non-major-city application unresolved; do not silently assert a three-unit cap. [RUL-GRA-08](2026-11-12-graziani-ruling-decisions.md#rul-gra-08-abstract-supply-stacking-bounded-planning-interpretation) remains a bounded project interpretation, and scenario setup, dummies, actual allocation and any exceptions require separate policy and review.

Independent review should verify the dot, coordinate and key, then test any admitted classification and supply-capacity interpretation separately. No runtime data, content, readiness flags or gameplay tests changed.
