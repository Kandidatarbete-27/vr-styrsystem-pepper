
# Pepper configs
LOCAL_IP = "0.0.0.0" # Dont change this, it is set to standard
LOCAL_PORT = 0 # Dont change this, it is set to 0 to get a random port
PEPPER_IP = "localhost" # IP to Pepper robot
PEPPER_PORT = 9559 # Port to Pepper robot, default is 9559, however check for specific robot

# Router configs
SERVER_IP =  "IP TO VR COMPUTER" #

"""
Dont touch the portnumbers,
they should be allowed in the firewall and router
and portforwarded to the computer running this code

common issues:
- portforwarding not set up correctly
- firewall blocking the ports
- wrong ip addresses
"""

PORT_INSTRUCTIONS = 55002 # 55002 instrunction
PORT_ADUIO = 55001  # 55001 audio feed
PORT_VIDEO = 55000  # 55000 video feed
