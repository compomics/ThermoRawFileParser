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


for content in loaded_json:
    if isinstance(content["mzs"], str):
        import base64
        import struct
        masses = base64.standard_b64decode(content["mzs"])
        intens = base64.standard_b64decode(content["intensities"])
        
        masses = struct.unpack('d' * (len(masses) >> 3), masses)
        intens = struct.unpack('d' * (len(intens) >> 3), intens)
        
        
    else:
        masses = content["mzs"]
        intens = content["intensities"]

    plt.plot(masses, intens)

fig.tight_layout()
plt.grid(True)
plt.show()
