import os
import sys
import fileinput
import fnmatch


lines = []
with open('readytoconvert.txt') as f:
    lines = f.readlines()

IFFACE = 0
IFEDGE = 0

for line in lines:
    NORM = f"{line}"
    if NORM.replace("f ", "") == NORM[2:]:
        IFFACE = 1

for line in lines:
    NORM = f"{line}"
    if NORM.replace("l ", "") == NORM[2:]:
        IFEDGE = 1
print (IFEDGE)
print (IFFACE)
#if IFFACE == 1:
#    print ("hehe")

#os.system("pause")
#os.system("cls")


count = 0
VERTSL = []
print ("vert points")
for line in lines:
    count += 1
    NORM = f"{line}"
    #print(NORM)
    if NORM.replace("v ", "") == NORM[2:]:
        print(NORM)
        VERTSL.extend ([count])

print ("on lines",VERTSL)


count = 0

if IFFACE == 1:
    FACESL = []
    print ("faces")
    for line in lines:
        count += 1
        NORM = f"{line}"
        print(NORM)
        if NORM.replace("f ", "") == NORM[2:]:
            print(NORM)
            FACESL.extend ([count])



count = 0    

if IFEDGE == 1:
    EDGESL = []
    for line in lines:
        count += 1
        NORM = f"{line}"
        print(NORM)
        if NORM.replace("l ", "") == NORM[2:]:
            print(NORM)
            EDGESL.extend ([count])




if IFFACE == 1:
    print (FACESL[0], "and", FACESL[-1])
if IFEDGE == 1:
    print (EDGESL[0], "and", EDGESL[-1])

#os.system("pause")



#-------------------------------------------ABOVE IS GETTING INFO ABOUT THE FILE
#-------------------------------------------BELOW IS MAKING THE FILE

os.system("if exist 3DG1.txt del 3DG1.txt")


videoscape = open("3DG1.txt", "w")
videoscape.write("3DG1 \n")

vertamount = VERTSL[-1]-VERTSL[0]+2


videoscape.write(str(vertamount))
videoscape.write("\n")
videoscape.write("0.00000 0.000000 0.000000")
videoscape.write("\n")


count = 0
for line in lines:
    count += 1
    NORM = f"{line}"
    #print(NORM)
    if NORM.replace("v ", "") == NORM[2:]:
        NORM = NORM[2:]
        print(NORM)
        videoscape.write(NORM)
        
        




os.system("cls")

if IFFACE == 1:
    OVERALLRANGE=1000
    
    countA=int(0)
    count = 0
    rangestuff=int(FACESL[-1])
    
    for countA in range(OVERALLRANGE):
        countA +=1
        
        
        BAK = int(OVERALLRANGE-countA)
        #print(BAK)
        
        
        REP ="/" + str(BAK)
        REP2 ="//" + str(BAK)
        REP =str(REP)
        REP2 =str(REP2)
        #line=line.replace(REP, "")
        count = 0
        
        for count in range(rangestuff):
            #count += 1
            TEMP = lines[count]
            TEMP = TEMP.replace(REP2, "")
            TEMP = TEMP.replace(REP, "")
            
            lines[count] = TEMP
    
    
    #print (REP)

    print (lines)


    count = 0  
    count2 = 0
    START = FACESL[0]
    END = FACESL[-1]+1
    os.system("cls")
    
    print(START)
    print(END)
    
    os.system("pause")

    dummy =str(1)

    print ("start of fun")
    for line in lines:
        count +=1
        #for count in range(START,END):
        if count in range(START,END):
            count2 += 1
            readytoprint = f"{line}"
            readytoprint = readytoprint[2:]
            #readytoprint = readytoprint.replace("1/*/*","")
            #readytoprint = readytoprint.replace(dummy,"E")
            FACEAM = readytoprint.count(' ')
            FACEAM = FACEAM+1
            
            #ADD COLOUR STUFF HERE
            
    
            #FINAL
            
            #countA=0
            #countB=0
            #for countA in range(int(OVERALLRANGE)):
            #    countA += 1
            #    print (countA)
    
            #print (FACEAM)
            print (FACEAM, readytoprint)
            videoscape.write(str(FACEAM) + " " + str(readytoprint[:-1]) + " " + "0\n")
            
            #videoscape.write("\n")
if IFEDGE == 1:        
    count = 0
    for line in lines:
        count += 1
        NORM = f"{line}"
        #print(NORM)
        if NORM.replace("l ", "") == NORM[2:]:
            NORM = NORM[2:]
            NORM = "2 " + NORM[:-1] + " 0\n"
            print(NORM)
            videoscape.write(NORM)


videoscape.close()
#os.system("pause")
