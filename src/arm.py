import naoqi as nq
import config as cfg
import time

"""
A PD regulator for the arms are implemented but not used in this version.
The regulator is not correctly implemented.
Code will be left here for future reference and improvements.
"""

MIN_SPEED = 0.2
MAX_SPEED = 0.6

ARM_SPEED = 0.4
REGULATOR = False
KP = 0.3
KD = 0.1
EPSILON = 1e-6

# Class for controlling the left arm of Pepper
class LArm:
    def __init__(self, queue):
        self.angleValues = [0] * 6
        self.angleNames = ["LShoulderPitch", "LShoulderRoll", "LElbowYaw", "LElbowRoll", "LWristYaw", "LHand"]
        self.angleBounds = {"LShoulderPitch": (-2.0857, 2.0857), "LShoulderRoll": (0.0087, 1.5620),
                            "LElbowYaw": (-2.0857, 2.0857), "LElbowRoll": (-1.5620, -0.0087),
                            "LWristYaw": (-1.8238, 1.8238)} 
        self.angleSpeeds = [ARM_SPEED] * 6
        
        if REGULATOR:
            self.Kp = KP
            self.Kd = KD
            
            self.previous_angleValues = [0] * 6
            self.previous_error = [0] * 6
            self.previous_time = time.time()
            self.current_time = None
            

        self.motion_handler = nq.ALProxy("ALMotion", cfg.PEPPER_IP, cfg.PEPPER_PORT)
        self.queue = queue
        self.hand_open = True

        self.main()

    def move(self, data, ):
        hand_pos = data.pop("LHand")
        #print(data)
        for i, value in enumerate(self.angleNames):
            if value == "LHand":
                continue
            self.angleValues[i] = data[value]

            if value in self.angleBounds:
                min_val, max_val = self.angleBounds[value]
                if self.angleValues[i] < min_val:
                    self.angleValues[i] = min_val
                elif self.angleValues[i] > max_val:
                    self.angleValues[i] = max_val

        if REGULATOR:
            # PD controller
            self.current_time = time.time()
            dt = self.current_time - self.previous_time
            dt = max(dt, EPSILON)
            self.previous_time = self.current_time
            for i in range(len(self.angleSpeeds)):
                error = self.angleValues[i] - self.previous_angleValues[i]
                derivate = (error - self.previous_error[i]) / dt
                self.angleSpeeds[i] = self.Kp * error + self.Kd * derivate
                self.previous_error[i] = error
                self.previous_angleValues[i] = self.angleValues[i]
                
                if self.angleSpeeds[i] < MIN_SPEED:
                    self.angleSpeeds[i] = MIN_SPEED
                elif self.angleSpeeds[i] > MAX_SPEED:
                    self.angleSpeeds[i] = MAX_SPEED
        
        
        # Set the values as speeds
        self.motion_handler.setAngles(self.angleNames, self.angleValues, self.angleSpeeds)

        if hand_pos == 1 and not self.hand_open:
            self.motion_handler.openHand("LHand")
            self.hand_open = True
        elif hand_pos == 0 and self.hand_open:
            self.motion_handler.closeHand("LHand")
            self.hand_open = False 


    def main(self):
        while True:
            if not self.queue.empty():
                data = self.queue.get()
                if data == "stop":
                    break
                self.move(data)

# Class for controlling the right arm of Pepper
class RArm:
    def __init__(self, queue):
        self.angleValues = [0] * 6
        self.angleNames = ["RShoulderPitch", "RShoulderRoll", "RElbowYaw", "RElbowRoll", "RWristYaw", "RHand"]
        self.angleBounds = {"RShoulderPitch": (-2.0857, 2.0857), "RShoulderRoll": (-1.5620, -0.0087),
                            "RElbowYaw": (-2.0857, 2.0857), "RElbowRoll": (0.0087, 1.5620),
                            "RWristYaw": (-1.8238, 1.8238)} 
        self.angleSpeeds = [ARM_SPEED] * 6

        if REGULATOR:
            self.Kp = KP
            self.Kd = KD

            self.previous_angleValues = [0] * 6
            self.previous_error = [0] * 6
            self.previous_time = time.time()
            self.current_time = None
            

        self.motion_handler = self.motion_handler = nq.ALProxy("ALMotion", cfg.PEPPER_IP, cfg.PEPPER_PORT)
        self.queue = queue
        self.hand_open = True

        self.main()

    def move(self, data):
        hand_pos = data.pop("RHand")
        for i, value in enumerate(self.angleNames):
            if value == "RHand":
                continue
            self.angleValues[i] = data[value]

            if value in self.angleBounds:
                min_val, max_val = self.angleBounds[value]
                if self.angleValues[i] < min_val:
                    self.angleValues[i] = min_val
                elif self.angleValues[i] > max_val:
                    self.angleValues[i] = max_val
        
        
        if REGULATOR:
            # PD controller
            self.current_time = time.time()
            dt = self.current_time - self.previous_time
            dt = max(dt, EPSILON)
            self.previous_time = self.current_time
            for i in range(len(self.angleSpeeds)):
                error = self.angleValues[i] - self.previous_angleValues[i]
                derivate = (error - self.previous_error[i]) / dt
                self.angleSpeeds[i] = self.Kp * error + self.Kd * derivate
                self.previous_error[i] = error
                self.previous_angleValues[i] = self.angleValues[i]
                
                if self.angleSpeeds[i] < MIN_SPEED:
                    self.angleSpeeds[i] = MIN_SPEED
                elif self.angleSpeeds[i] > MAX_SPEED:
                    self.angleSpeeds[i] = MAX_SPEED
            

        self.motion_handler.setAngles(self.angleNames, self.angleValues, self.angleSpeeds)

        if hand_pos == 1 and not self.hand_open:
            self.motion_handler.openHand("RHand")
            self.hand_open = True
        elif hand_pos == 0 and self.hand_open:
            self.motion_handler.closeHand("RHand")
            self.hand_open = False 


    def main(self):
        while True:
            if not self.queue.empty():
                data = self.queue.get()
                if data == "stop":
                    break
                self.move(data)
                