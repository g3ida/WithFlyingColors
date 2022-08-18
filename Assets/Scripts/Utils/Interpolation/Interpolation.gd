func _init():
	pass	

func apply (start: float, end: float, a: float) -> float:
	return start + (end - start) * _apply(a);
	
func _apply(a: float) -> float:
	return 0.0
