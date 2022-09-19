class_name Helpers

static func sign_of(x: float) -> float: return -1.0 if x < 0 else 1.0

static func intersects(circlePos: Vector2, circleRadius: float, rectPos: Vector2, rect: Vector2):
  var circle_dist = Vector2()
  circle_dist.x = abs(circlePos.x - rectPos.x)
  circle_dist.y = abs(circlePos.y - rectPos.y)
      
  if (circle_dist.x > (rect.x/2.0 + circleRadius)): return false
  if (circle_dist.y > (rect.y/2.0 + circleRadius)): return false
      
  if (circle_dist.x <= (rect.x/2.0)): return true; 
  if (circle_dist.y <= (rect.y/2.0)): return true;
      
  var cornerDistance_sq = pow((circle_dist.x - rect.x/2), 2) + pow((circle_dist.y - rect.y/2), 2)  
  return (cornerDistance_sq <= (circleRadius*circleRadius))

static func array_contains_scene_type(array, scene):
  for el in array:
    if scene is el:
      return true
  return false
