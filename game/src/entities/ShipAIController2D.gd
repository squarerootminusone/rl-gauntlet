extends AIController2D
class_name ShipAIController

@export var ship: Node2D

func get_obs() -> Dictionary:
	return {"obs":[
		ship.global_position.x,
		ship.global_position.y
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
	ship.SetTargetPosition(Vector2(action["move"][0] * 2000, action["move"][1]) * 2000)
