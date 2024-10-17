# GOALLM_v2
GOAP+LLM Ver.2

### This version contains following prompt:

  + Gesture: <Gesture from emotion of {{char}} currently feeling>
  
  + Think: <A short sentence summarizing what {{char}} is thinking>
  
  + Talk Goal: <A short sentence describing the purpose of {{char}}'s speech>
  
  + Move Goal: <Place that {{char}} want to go>
  
  + Item Goal: <"Pick up" or "Drop" or "Use"> <Item that {{char}} want to do someting> at <(Optional) Place that {{char}} want to do someting>




### GOAP can make plan with this kind of actions:

  + eat_snack: conditions={"hold": "snack"}, effects={"use_item": "snack", "hold": "none", "used_snack": True}, cost
  
  + open_door: conditions={"location": "door","door_closed": lambda npc_state, world_state: world_state.places["door"].state.get("door_state") == "closed"}, effects={"place_state:door:door_state": "open"},cost

  + greet: conditions={"hold": "none","pose": "stand"}, effects={"did_greet": True}, cost

  + sit_sofa: conditions={"location": "sofa","pose": "stand"}, effects={"pose": "sit"}, cost

  + stand_sofa: conditions={"location": "sofa","pose": "sit"}, effects={"pose": "stand"}, cost

Also move, drop_item, pick_item


(Working Image)
![image](https://github.com/user-attachments/assets/90bb6956-e526-4e43-9928-aaf1e8fa4ff1)
