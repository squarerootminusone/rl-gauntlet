extends AIController2D
class_name ShipAIController2D

@export var ship: Node2D

# Access GameSceneManager from ship (ship.GameSceneManager is set during initialization)
func get_obs() -> Dictionary:
	return {"obs":[
		ship.global_position.x,
		ship.global_position.y,
		910,
		476
	]}

func get_reward() -> float:	
	return reward
	
func get_action_space() -> Dictionary:
	return {
		"move" : {
			"size": 2,
			"action_type": "continuous"
		},
		"toggle_mine": {
			"size": 2,
			"action_type": "discrete"
		}
	}
	
func set_action(action) -> void:	
	ship.SetTargetPosition(Vector2(action["move"][0] * 2000, action["move"][1] * 2000))
	ship.SetRequestMine(action["toggle_mine"] == 0)
