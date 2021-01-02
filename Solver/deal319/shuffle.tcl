stack_hand north {QJ32 AQJ2 A32 K2}
stack_cards south spades AK diamonds K 
shapeclass exact_shape {expr $s==5 && $h==4 && $d==1 && $c==3}
deal::input smartstack south exact_shape controls 4 4

main {
	accept
}
