defvector controls 2 1
holdingProc DCB1 {A K} {
   if {$A + $K == 1} {return 1}
   return 0
}
defvector DCB2 0 0 1
stack_hand north {KT98 AK96 AKJ9 4}
shapeclass south_shape { expr $s == 4 && $h == 4 && $d == 2 && $c == 3 }
deal::input smartstack south south_shape controls 2 3
smartstack::restrictHolding spades DCB1 1 1
smartstack::restrictHolding hearts DCB1 0 0
smartstack::restrictHolding diamonds DCB1 0 0
smartstack::restrictHolding clubs DCB1 1 1
smartstack::restrictHolding spades DCB2 0 0
smartstack::restrictHolding hearts DCB2 1 1
smartstack::restrictHolding diamonds DCB2 0 0
smartstack::restrictHolding clubs DCB2 1 1
