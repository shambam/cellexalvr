import anndata
import sys
import numpy

def crawl(lib, push):
	key_value = {}
	for a in lib.keys():
		if(hasattr(lib[a],'shape')):
			key_value[a] = lib[a].shape
			print(push + a + ": " + str(lib[a].shape) + "_"+ str(lib[a].dtype.kind))
		else:
			print(push + a + ":")
			key_value[a] = crawl(lib[a], push + str(a)+"/")
	return key_value
	
	
file_name = sys.argv[1]
f = anndata.h5py.File(file_name,'r')
if(len(sys.argv) < 3):
	mappen = crawl(f, "")
else:
	try:
		if(hasattr(f[sys.argv[2]],'shape')):
			print(f[sys.argv[2]][:])
		else:
			crawl(f[sys.argv[2]],"")
	except:
		print("name not found")
