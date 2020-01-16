import json
import sys
import numpy as np
import matplotlib.pyplot as plt

if len(sys.argv) < 2:
    print("usage:", sys.argv[0], "input-json")
    exit()

with open(sys.argv[1], 'r') as f:
    loaded_json = json.load(f)
 
    
fig = plt.figure()

#base64.standard_b64decode(s)¶

for content in loaded_json["content"]:
    if isinstance(content["X"], str):
        import base64
        import struct
        times = base64.standard_b64decode(content["X"])
        intens = base64.standard_b64decode(content["Y"])
        
        times = struct.unpack('d' * (len(times) >> 3), times)
        intens = struct.unpack('d' * (len(intens) >> 3), intens)
        
        
    else:
        times = content["X"]
        intens = content["Y"]

    plt.plot(times, intens)

fig.tight_layout()
plt.grid(True)
plt.show()
