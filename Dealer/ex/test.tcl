main {
					  # Pitch deals
					  # where north does
					  # not have four spades
	reject if {[north shape]!={4 4 2 3}}
    reject if {[north has AS QH KC QC] != 4}
    reject if {[north has KS QS AH KH AD KD QD AC]}
    accept
}