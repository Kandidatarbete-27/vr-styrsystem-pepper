import naoqi as nq
import config as cfg
import math
import time

FIXED_HEIGHT = True # Set to True if you want to fix the height of the robot

class Movement:
    def __init__(self, queue):
        self.motion_handler = nq.ALProxy("ALMotion", cfg.PEPPER_IP, cfg.PEPPER_PORT)
        self.queue = queue
        self.cords = [0] * 4
        self.cords_names = ["x", "y", "theta", "z"]
        self.coordinates_error = [0] * 3
        self.core = []
        self.start_position = self.motion_handler.getRobotPosition(False)
        self.speed = [0] * 3 
        self.want_pos = [0] * 3
        self.position = [0] * 3
        self.prev_position = [0] * 3
        self.prev_time = time.time()

        if FIXED_HEIGHT:
            # turn of self balancing
            #self.motion_handler.setStiffnesses("Body", 1)  # turn off stiffness
            print("Fixed height")
            # set stiffness of body to 0.0 to turn off self balancing

        self.main()

    def read_position(self):
        return self.motion_handler.getRobotPosition(False)  # could be true
    
    # Make sure not to rotate wrong direction becasue of -180 - 180 degrees.
    def fix_rotation(self, want, current):
        if abs(want - current) > math.pi:
            if current > want:
                return want - (current - 2*math.pi) 
            else: 
                return want - (current + 2*math.pi) 
        else:
            return want - current

    def calculate_coordinates_error(self, position, want_pos):
        # Calculate the error in the global coordinate system
        global_error = [
            want_pos[0] - position[0] + self.start_position[0],
            want_pos[1] - position[1] + self.start_position[1],
            #want_pos[2] - position[2]
            self.fix_rotation(want_pos[2], position[2])
        ]

        # Rotate the global error into the robot's local coordinate system
        theta = position[2]  # Current orientation of the robot
        cos_theta = math.cos(-theta)
        sin_theta = math.sin(-theta)

        self.coordinates_error = [
            global_error[0] * cos_theta - global_error[1] * sin_theta,
            global_error[0] * sin_theta + global_error[1] * cos_theta,
            global_error[2]  # Orientation error remains unchanged
            
        ]

    def move(self, data):
        #print("in move")
        # maybe handle data
        self.want_pos = [data["z"], data["x"], data["theta"]]
        self.position = self.read_position()
        #self.position[2] = self.
        self.calculate_coordinates_error(self.position, self.want_pos)
        
        #PD controller variables
        kp_xy = 0.5
        kd_xy = 0.1
        kp_theta = 0.5
        kd_theta = 0.05 
        margin = 0.1

        current_time = time.time()
        dt = max(current_time - self.prev_time, 0.01)  # avoid div by zero

        dx = (self.position[0] - self.prev_position[0]) / dt
        dy = (self.position[1] - self.prev_position[1]) / dt
        dtheta = (self.position[2] - self.prev_position[2]) / dt
        
        if abs(self.coordinates_error[0]) < margin:
            self.speed[0] = 0
        else:
            self.speed[0] = kp_xy * self.coordinates_error[0] - kd_xy * dx
        if abs(self.coordinates_error[1]) < margin:
            self.speed[1] = 0
        else:
            self.speed[1] = kp_xy * self.coordinates_error[1] - kd_xy * dy
        if abs(self.coordinates_error[2]) < margin:
            self.speed[2] = 0
        else:
            self.speed[2] = kp_theta * self.coordinates_error[2] - kd_theta * dtheta

        #print("position", self.position[2])
        #print("Want position", self.want_pos[2])
        #print("Speed", self.speed[2])

        self.prev_time = current_time
        self.prev_position = self.position
        self.motion_handler.move(self.speed[0], self.speed[1], self.speed[2])

        #fix height
        if FIXED_HEIGHT: 
            #print("Fixing height")
            data_names = ["HipRoll", "HipPitch", "KneePitch"]
            values = [0] * 3
            speeds = [0.75] * 3
            self.motion_handler.setAngles(data_names, values, speeds)  # set angles to 0
            #self.motion_handler.setStiffnesses(data_names, 1) # set stiffness to 0.0 to turn off self balancing
        #set position of height too, "y" is the height


    def main(self):
        last_data = {"x": 0, "y": 0, "theta": 0, "z": 0}
        while True:
            self.move(last_data)
            if not self.queue.empty():
                data = self.queue.get()
                last_data = data
                if data == "stop":
                    break
                self.move(data)