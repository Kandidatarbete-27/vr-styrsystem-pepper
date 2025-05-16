import socket
import cv2
import struct
import json
#import keyboard

class ComSender:
    def __init__(self, SERVER_IP, PORT):
        

        self.client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.client_socket.connect((SERVER_IP, PORT))

    def send_frame(self, frame):
        # Encode the frame as JPEG
        _, encoded_frame = cv2.imencode('.jpg', frame)
        data = encoded_frame.tobytes()
        # Pack the length of the data into 4 bytes
        data_len = struct.pack('>I', len(data))
        # Send the length of the data first
        self.client_socket.sendall(data_len)
        # Send the actual data
        self.client_socket.sendall(data)
        #print("frame sent")
    
    def close(self):
        self.client_socket.close()

    def send_audio(self, audio):
        pass
            
        self.client_socket.sendall(struct.pack('>I', len(audio)))
        self.client_socket.sendall(audio)



class ComReceiver:
    def __init__(self, SERVER_IP, PORT):
        self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.server_socket.bind((SERVER_IP, PORT))
        self.server_socket.listen(1)
        print("Server listening on {}:{}".format(SERVER_IP, PORT))
        self.conn, self.addr = self.server_socket.accept()
        print("Connected by {}".format(self.addr))

    def receive_data(self):
        try:
            data = self.conn.recv(12)  # Receive 12 bytes (3 floats, 4 bytes each)
            if not data:
                print("No data received")
                return None
            if len(data) == 12:
                # Unpack the 12 bytes into 3 floats (x, y, z)
                x, y, z = struct.unpack('fff', data)
                #print("Received data: x={}, y={}, z={}".format(x, y, z))
                return x, y, z
            else:
                print("Received data of unexpected length: {}".format(len(data)))
                return None
        except Exception as e:
            print("Error receiving data:", e)
            return None
        
    def receive_json_first(self):
        try:
            data = self.conn.recv(4)
            
            while True:
                if not data:
                    print("No data received")
                    return None
                else:
                    data_len = struct.unpack('>I', data)[0]
                    data = self.conn.recv(data_len)
                    if len(data) == data_len:
                        return data
                    else:
                        print("Received data of unexpected length: {}".format(len(data)))
                        return None
                
        except Exception as e:
            print("Error receiving data:", e)
            return None
        
    def receive_json_2(self):
        try:
            data = self.conn.recv(1024)
            data_dict = json.loads(data.decode('utf-8'))
            
            return data_dict
                
        except Exception as e:
            print("Error receiving data:", e)
            return None
        
    def receive_json_3(self):
        try:
            # First, receive the length of the incoming JSON data
            #self.conn.settimeout(1)
            data_len_bytes = self.conn.recv(4)
            if not data_len_bytes:
                print("No data received")
                return None
            
            data_len = struct.unpack('>I', data_len_bytes)[0]
            data = b''
            
            # Receive the data in chunks until the entire message is received
            while len(data) < data_len:
                packet = self.conn.recv(data_len - len(data))
                if not packet:
                    print("No data received")
                    return None
                data += packet
            
            # Decode and parse the JSON data
            data_dict = json.loads(data.decode('utf-8'))
            return data_dict
                
        except Exception as e:
            print("Error receiving data:", e)
            return None
        
    def receive_json(self):
        
        data_len = struct.unpack('>I', self.conn.recv(4))[0]
        data = b''
        while len(data) < data_len:
            packet = self.conn.recv(data_len - len(data))
            if not packet:
                break
            data += packet
        
        #Convert the bytes back to a string
        data_dict = json.loads(data.decode('utf-8'))
        #print("Data received:" + data_dict)
        return data_dict
        
    def close(self):
        self.conn.close()
        self.server_socket.close()

if __name__ == "__main__":
    print("before socket")
    com_receiver = ComReceiver("0.0.0.0", 55002)
    print("after socket")
    try:
        while True:
            data = com_receiver.receive_json()
            print(data)
    except Exception as e:
        print("An error occurred")
        print(e)
    finally:
        com_receiver.close()
        exit(0)