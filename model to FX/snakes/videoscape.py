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
        
        




#os.system("cls")
#print (IFFACE)
#print (IFEDGE)
#os.system("pause")

#IFFACE = 1

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
    
    

    COLL = 0
    EDGECOLL = 0
    count = 0  
    count2 = 0
    START = FACESL[0]-2
    END = FACESL[-1]+1
    os.system("cls")
    
    print(START)
    print(END)
    
    #os.system("pause")

    dummy =str(1)

    print ("start of fun")
    
    for line in lines:
        count +=1
        #for count in range(START,END):
        if count in range(START,END):
            count2 += 1
            
            if count2 == 4:
                videoscape.write("\n")
                #this makes things work, i don't know why
            
            readytoprint = f"{line}"
            
            if readytoprint[0:2] == "un":
                print ("FOUND VN INSTEAD")
            
            elif readytoprint[0:1] == "s":
                print("FOUND s THING INSTEAD")
            
            elif readytoprint[0:1] == "g":
                print("FOUND g THING INSTEAD")
            
            elif readytoprint[0:9] == "usemtl FE":
                EDGECOLL = readytoprint.replace("usemtl FE","")
            
            elif readytoprint[0:9] == "usemtl FX":
                COLL = readytoprint.replace("usemtl FX","")
            
            elif readytoprint[0:6] == "usemtl":
                os.system("cls")
                print ("MATERIAL NAME USES INCORRECT FORMAT")
                print ("USE FX1 FX2 FX3 etc")
                os.system("pause")
            
            
            
            else:
            
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
                readytoprint = readytoprint.replace("\n","")
                print (FACEAM, readytoprint)
                #os.system("pause")
                #str(readytoprint[:-1])
                videoscape.write(str(FACEAM) + " " + str(readytoprint) + " " + str(COLL) + "\n")
            
            #videoscape.write("\n")

#os.system("cls")
print(IFEDGE)



if IFEDGE == 1:        
    count = 0
    #os.system("pause")
    for line in lines:
        count += 1
        NORM = f"{line}"
        print(NORM)
        #os.system("pause")
        
        if NORM.replace("l ", "") == NORM[2:]:
            NORM = NORM[2:]
            NORM = "2 " + NORM[:-1] + " " + str(EDGECOLL) + "\n"
            print(NORM)
            videoscape.write(NORM)
                


videoscape.close()
#os.system("pause")
