import os
import sys
import fileinput

with open('readytoconvert.txt', 'r') as file :
    filedata = file.read()
    

MAXN = 250
MAXD = 50

#MAXN = highest value
#MAXD = range of error

filedata = filedata.replace('000001', '000000') 
filedata = filedata.replace('000002', '000000') 
filedata = filedata.replace('000003', '000000') 
filedata = filedata.replace('000004', '000000') 
filedata = filedata.replace('000005', '000000') 
filedata = filedata.replace('000006', '000000') 
filedata = filedata.replace('000007', '000000') 
filedata = filedata.replace('000008', '000000') 
filedata = filedata.replace('000009', '000000') 
filedata = filedata.replace('000010', '000000') 
filedata = filedata.replace('000011', '000000') 
filedata = filedata.replace('000012', '000000') 
filedata = filedata.replace('000013', '000000') 
filedata = filedata.replace('000014', '000000') 
filedata = filedata.replace('000015', '000000') 
filedata = filedata.replace('000016', '000000') 
filedata = filedata.replace('000017', '000000') 
filedata = filedata.replace('000018', '000000') 
filedata = filedata.replace('000019', '000000') 
filedata = filedata.replace('000020', '000000')
filedata = filedata.replace('000021', '000000')
filedata = filedata.replace('000022', '000000')
filedata = filedata.replace('000023', '000000') 
filedata = filedata.replace('000024', '000000')
filedata = filedata.replace('000025', '000000') 

countA = MAXN
countB = 999999

for countA in range(MAXN):
    countB = 999999
    countA -= 1
        for countB in range(MAXD):
           filedata = filedata.replace('200.999999', '201.000000') 
           
 





