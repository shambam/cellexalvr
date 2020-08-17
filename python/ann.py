import anndata
import sys
import numpy as np
import json

np.set_printoptions(threshold=sys.maxsize,linewidth=np.inf)
file_name = sys.argv[1]
if file_name.endswith(".loom"):
    f = anndata.h5py.File(file_name,'r')
else:
    f = anndata.read_h5ad(file_name,'r')
while True:
    interp = input()
    try:
        exec("print(json.dumps("+interp+"))")
    	#exec("print(np.array2string("+interp+", separator=','))")
    except Exception as e:
    	print(e)
    sys.stdout.flush()
