import socket
import sounddevice
import numpy as np

# Define server address and port
HOST = "0.0.0.0"  # Listen on all available interfaces
PORT = 55000  # Choose an available port

# Create a UDP socket
server_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Bind the socket to the address and port
server_socket.bind((HOST, PORT))

print("UDP Server listening on {}:{}".format(HOST, PORT))

stream = sounddevice.OutputStream(samplerate=16000, channels=1, dtype='float32')
print("Stream created")
stream.start()

while True:
    # Receive data from client
    data, addr = server_socket.recvfrom(65507)  # Buffer size 1024 bytes
    print("Got data")
    #print("Received message from {}: {}".format(addr, data))
    #stream.write(np.array(eval(data.decode("utf-8")), dtype='float32'))
    print(data.decode("utf-8"))
    np.array(eval(data.decode("utf-8")), dtype='float32')