import os
import sys
import fileinput


#with open('readytoconvert.txt', 'r') as file :
#    filedata = file.read()

lines = []
with open('readytoconvert.txt') as f:
    lines = f.readlines()
    vertam = int(lines[1]) #VERT AMOUNT

count = 0
for line in lines:
    count += 1
    print(f'line {count}: {line}')
    print(count)
    
    

os.system("cls")

#range(2,vertam):

os.system("if exist VERTS.bat del VERTS.bat")
Vertbat = open("VERTS.bat", "w")
count = 0
countR = 0
for line in lines:
    count += 1
    if count in range(4,vertam+3):
        countR += 1
        #print(f"setG{countR}={line}")
        #stringthing = f"set {countR}={line}"
        stringthing = f"{countR}={line}"
        stringthing2 = stringthing.replace(".000000", "")
        
        stringthingE = f"{line}"
        stringthingE2 = stringthingE.replace(".000000", "")
        stringthingE3 = stringthingE2.replace(" ", ",")
        
        stringthing3 = stringthing2.replace(" ", ",")
        print("set", stringthing3)
        Vertbat.write("set ")
        Vertbat.write(stringthing3)
        os.environ [str(countR)] =stringthingE3[:-1]

Vertbat.close()
        
    
print ("VERTS STUFF")

os.system("call VERTS")
os.system("cls")

# NOT SURE WHAT THIS JUNK IS
#count = 0
#for line in lines:
#    count += 1
#    print(f'line {count}: {line}') 
#    if count == vertam:
#        break


MAXcount = 0
for line in lines:
    MAXcount += 1
    

    
count = 0
countE = 0

os.system("if exist FACES.bat del FACES.bat")
os.system("if exist OUTPUT.txt del OUTPUT.txt")
Facebat = open("FACES.bat", "w")
os.system("echo mesh {>>OUTPUT.txt")

for line in lines:
    count += 1
    if count in range(3+vertam,MAXcount+1):
        #conutE += 1
        stringthing = f"{line}"
        if stringthing[0:1] == 3:
            stringthing2 = stringthing.replace("3 ", "", 1)
            
        stringthing3 = stringthing2.replace(" 0", "", 1)
        stringthing3 = stringthing3.replace(" " ,"% %")
        stringthing3 = stringthing3.replace(" " ,"^>, ^<")
        
            
        #print (stringthing3)
        
        
        print("triangle{ ^<%",stringthing3[:-1],"%^>","}>>OUTPUT.txt",sep='')
        #Facebat.write("%",stringthing3[:-1],"%",sep='')
        #crud = "echo triangle {^<%" + stringthing3[:-1] +"%^>" + "}>>OUTPUT.bat"
        crud = "echo triangle {^<%" + stringthing3[:-1] +"%^>" + "}>>OUTPUT.txt"
        #Facebat.write(crud)
        #Facebat.write("\n")
        os.system(crud)
        
#-------
stringthing = f"{line}"
stringthing2 = stringthing.replace("3 ", "", 1)
stringthing3 = stringthing2.replace(" 0", "", 1)
stringthing3 = stringthing3.replace(" " ,"% %")
stringthing3 = stringthing3.replace(" " ,"^>, ^<")
print("triangle{ ^<%",stringthing3,"%^>","}>>OUTPUT.txt",sep='')
crud = "echo triangle {^<%" + stringthing3 +"%^>" + "}>>OUTPUT.txt"
os.system(crud)

os.system("echo pigment {rgb 1}>>OUTPUT.txt")
os.system("echo }>>OUTPUT.txt")

Facebat.close()



