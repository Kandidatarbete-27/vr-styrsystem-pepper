import random
import motion
import config as conf
import naoqi


def set_motion_data(data):
    for i, value in enumerate(data):
        data[i] += random.choice([-1,1]) * 0.01


def predetermine_motion_data(data):
        data[2] += 0.01

if __name__ == "__main__":
    """ motion = Motion(conf.BROKER, conf.PEPPER_IP, conf.PEPPER_PORT)
    motion.setup()
    motion.loop()
    motion.shutdown() """
    BROKER = naoqi.ALBroker("pythonBroker", conf.LOCAL_IP, conf.LOCAL_PORT, conf.PEPPER_IP, conf.PEPPER_PORT) 
    motion = motion.Motion(BROKER, conf.PEPPER_IP, conf.PEPPER_PORT)
    data = motion.position_data
    set_motion_data(data)
    print(data)    