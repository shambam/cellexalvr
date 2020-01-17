suppressMessages(library(cellexalvrR))
message( "Start the server")

args <- commandArgs(trailingOnly = TRUE)
name <- args[1] ## name of the server folder

dataSourceFolder <- args[2]

outputFolder <- args[3]

if ( file.exists(file.path(dataSourceFolder, "cellexalObj.RData.lock") ) ){
	## oops - some problems in the last session?
	file.remove(file.path(dataSourceFolder, "cellexalObj.RData.lock") )
}
cellexalObj <- loadObject(file.path(dataSourceFolder, "cellexalObj.RData"))

cellexalObj@outpath = outputFolder

if ( ! file.exists(outputFolder ) ){
	dir.create( outputFolder )
}

cellexalObj <- renew(cellexalObj)

cellexalObj@outpath = outputFolder

#exportUserGroups4vr(cellexalObj, outputFolder)

server(name)