import anndata
import sys
import numpy as np
import json

np.set_printoptions(threshold=sys.maxsize,linewidth=np.inf)
file_name = sys.argv[1]
f = anndata.h5py.File(file_name,'r')
temp = ""
while True:
    interp = input()
    try:
        exec('print(json.dumps(' + interp + '))')
    except Exception as e:
    	print(e)
    sys.stdout.flush()
