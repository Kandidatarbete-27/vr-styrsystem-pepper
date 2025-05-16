import naoqi
#import keyboard
import multiprocessing as mp
import head
import arm
import movement

#GLOBAL VARIABLE FOR DEBUGGING
_HEAD = True
_R_ARM = True
_L_ARM = True
_MOVEMENT = True


class Motion:
    # Init 
    def __init__(self, broker, ip, port):
        self.broker = broker
        try:
            self.handler = naoqi.ALProxy("ALMotion", ip, port)
        except Exception as e:
            print("Could not create proxy to ALMotion in motion.py")
            print(e)
                

        if _HEAD:
            self.head_queue = mp.Queue()
            self.head_process = mp.Process(target=head.Head, args=(self.head_queue,))

        if _R_ARM:
            self.right_arm_queue = mp.Queue()
            self.right_arm_process = mp.Process(target=arm.RArm, args=(self.right_arm_queue,))

        if _L_ARM:
            self.left_arm_queue = mp.Queue()
            self.left_arm_process = mp.Process(target=arm.LArm, args=(self.left_arm_queue,))

        if _MOVEMENT:
            self.movement_queue = mp.Queue()
            self.movement_process = mp.Process(target=movement.Movement, args=(self.movement_queue,))


        self.start()
        #self.shutdown()

    def start(self):
        print("Starting wake up")
        self.handler.wakeUp()
        if _HEAD:
            self.head_process.start()
        if _R_ARM:
            self.right_arm_process.start()
        if _L_ARM:
            self.left_arm_process.start()
        if _MOVEMENT:
            self.movement_process.start()

    def shutdown(self):
        self.handler.rest()

        if _HEAD:
            self.head_queue.put("stop", block=False)
        if _R_ARM:
            self.right_arm_queue.put("stop", block=False)
        if _L_ARM:
            self.left_arm_queue.put("stop", block=False)
        if _MOVEMENT:
            self.movement_queue.put("stop", block=False)

        if _HEAD:
            self.head_process.join()
            self.head_queue.close()
            self.head_queue.join_thread()
        
        if _R_ARM:
            self.right_arm_process.join()
            self.right_arm_queue.close()
            self.right_arm_queue.join_thread()
        
        if _L_ARM:
            self.left_arm_process.join()
            self.left_arm_queue.close()
            self.left_arm_queue.join_thread()
        
        if _MOVEMENT:
            self.movement_process.join()
            self.movement_queue.close()
            self.movement_queue.join_thread()
        
        

            
    def set_motion_data(self, data):
        #print(data)
        #if data is None:
        #    print("no data recived before checking type....")
        #    return
        
        _type = data.pop("type")

        #print("Type: ", _type)

        if _type == "head" and _HEAD:
            self.head_queue.put(data, block=False)
            #print(data)
        elif _type == "rightArm" and _R_ARM:
            self.right_arm_queue.put(data, block=False)
        elif _type == "leftArm" and _L_ARM:
            self.left_arm_queue.put(data, block=False)
        elif _type == "move" and _MOVEMENT:
            self.movement_queue.put(data, block=False)
        else:
            print("Invalid data type, probably a type that is inactive")
        
"""
# Main loop
if __name__ == "__main__":
    LOCAL_IP = "0.0.0.0"
    LOCAL_PORT = 0
    PEPPER_IP = "192.168.0.106"
    #PEPPER_IP = "127.0.0.1" 
    PEPPER_PORT = 9559   
    #PEPPER_PORT = 57035
"""