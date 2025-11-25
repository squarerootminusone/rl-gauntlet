extends AIController2D
class_name PlayerAIController

@export var ship: CharacterBody2D

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
	ship._targetPosition = Vector2(action["move"][0], action["move"][1])
	ship._requestMine = true if action["mine"][0] == 0 else false
