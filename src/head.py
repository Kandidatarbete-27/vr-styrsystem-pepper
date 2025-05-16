import naoqi as nq
import yaml
import threading
import config as cfg

class Head:
    def __init__(self, queue):
        self.angleValues = [0] * 2
        self.angleNames = ["HeadYaw", "HeadPitch"]
        self.angleSpeeds = [0.1] * 2
        self.angleBounds = {"HeadYaw": (-2.0857, 2.0857), "HeadPitch": (-0.7068, 0.4451)}
        try:
            self.motion_handler = nq.ALProxy("ALMotion", cfg.PEPPER_IP, cfg.PEPPER_PORT)
        except Exception as e:
            print("Could not create proxy to ALMotion in head.py")
            print(e)
            return
        self.queue = queue
        

        self.main()

    def moveHead(self, data):
        # maybe handle data   
        for i, value in enumerate(self.angleNames):
            self.angleValues[i] = data[value]

            if value in self.angleBounds:
                min_val, max_val = self.angleBounds[value]
                if self.angleValues[i] < min_val:
                    self.angleValues[i] = min_val
                elif self.angleValues[i] > max_val:
                    self.angleValues[i] = max_val

        self.motion_handler.setAngles(self.angleNames, self.angleValues, self.angleSpeeds)
        
    def main(self):
        while True:
            if not self.queue.empty():
                data = self.queue.get()
                if data == "stop":
                    break
                self.moveHead(data)
            
