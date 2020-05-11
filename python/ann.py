import anndata
import sys
import numpy as np

np.set_printoptions(threshold=sys.maxsize,linewidth=np.inf)
file_name = sys.argv[1]
f = anndata.h5py.File(file_name,'r')
while True:
    interp = input()
    try:
        exec("print("+interp+")")
    	#exec("print(np.array2string("+interp+", separator=','))")
    except Exception as e:
    	print(e)
    sys.stdout.flush()
