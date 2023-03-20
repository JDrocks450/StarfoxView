import os
import sys
import fileinput

with open('readytoconvert.txt', 'r') as file :
    filedata = file.read()
    
filedata = filedata.replace('.000000', '')


with open('readytoconvert.txt', 'w') as file:
    file.write(filedata)