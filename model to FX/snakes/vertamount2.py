import os
import sys
import fileinput

debugfun = "false"

lines = []
with open('readytoconvert.txt') as f:
    lines = f.readlines()
    vertam = int(lines[1]) #VERT AMOUNT

#--------------- HOW LONG THE LIST IS
overalllength = 0
for line in lines:
    overalllength += 1



#--------------- SETTING VERTS INTO THE SET
count = 2
countR = 0
for count in range(2,vertam+2):
    stringthing = lines[count]
    stringthing = stringthing.replace(".000000", "")
    stringthing = stringthing.replace(".00000", "")
    stringthing = stringthing.replace("\n", "")
    stringthing = stringthing.replace(" ", ",")
    os.environ [str(countR)] =stringthing
    print (stringthing)
    #print("egg")
    count += 1
    countR += 1

#--------------- WHAT NEEDS TO BE MACROED
count = 0
triangles = "no"
quads = "no"
loselines = "no"

for count in range(vertam+2,overalllength):
    stringthing = lines[count]
    print (stringthing[0:1])
    if stringthing[0:1] == "3":
        triangles="yes"
    if stringthing[0:1] == "4":
        quads="yes"
    if stringthing[0:1] == "2":
        loselines="yes" 
    count += 1


#--------------- DEBUG FUN!
if debugfun == "true":
    print(vertam)
    print(overalllength)
    print("\n")
    print(triangles)
    print(quads)
    print(loselines)
    os.system("pause")
os.system("cls")

#------------------- OUTPUT TIME!!!!

os.system("if exist OUTPUT.txt del OUTPUT.txt")

output = open("OUTPUT.txt", "w")

#if has quads, make macro to handle them
if quads == "yes":
    output.write("#macro quad(V1,V2,V3,V4) triangle {V1,V2,V3} triangle {V1,V3,V4} #end \n")

#if has lines, make macro to handle them
if loselines == "yes":
    output.write("#macro line(V1,V2) #local thick=.03; merge {cylinder {V1, V2 thick} sphere{V1 thick} sphere{V2 thick}} #end \n")

if loselines == "yes":
    output.write("union { \n")


meshtime = "no"
if triangles == "yes":
    meshtime = "yes"
if quads == "yes":
    meshtime = "yes"



#output.write("mesh { \n")
output.close()

#---------------------- SLOW CMD OUTPUT NOW

count = vertam+3

if meshtime == "yes":
    os.system("echo mesh{>>OUTPUT.txt")
    for count in range(vertam+2, overalllength):
        stringthing = lines[count]
        
        #FORMAT FOR A TRIANGLE
        if stringthing[0:1] == "3": 
            stringthing = stringthing.replace("\n","")
            stringthing = stringthing[:-2]
            stringthing = stringthing.replace("3 ", "", 1)
            stringthing = stringthing.replace(" " ,"% %")
            stringthing = stringthing.replace(" " ,"^>, ^<")
            stringthing = f"^<%{stringthing}%^>"
            stringthing = "echo triangle{"+stringthing+"}>>OUTPUT.txt"
            os.system(stringthing)
            print (stringthing)
            stringthing = "DONE"
        
        if stringthing[0:1] == "4": 
            stringthing = stringthing.replace("\n","")
            stringthing = stringthing[:-2]
            stringthing = stringthing.replace("4 ", "", 1)
            stringthing = stringthing.replace(" " ,"% %")
            stringthing = stringthing.replace(" " ,"^>, ^<")
            stringthing = f"^<%{stringthing}%^>"
            stringthing = "echo quad("+stringthing+")>>OUTPUT.txt"
            os.system(stringthing)
            print (stringthing)
            stringthing = "DONE"
        
    
        #print (stringthing)
    
        count += 1
    os.system("echo pigment{rgb 1}>>OUTPUT.txt")
    os.system("echo }>>OUTPUT.txt")

count = vertam+3
if loselines == "yes":
    
    for count in range(vertam+2, overalllength):
        stringthing = lines[count]
        
        if stringthing[0:1] == "2":
            stringthing = stringthing.replace("\n","")
            stringthing = stringthing[:-2]
            stringthing = stringthing.replace("2 ", "", 1)
            stringthing = stringthing.replace(" " ,"% %")
            stringthing = stringthing.replace(" " ,"^>, ^<")
            stringthing = f"^<%{stringthing}%^>"
            stringthing = "echo line("+stringthing+")>>OUTPUT.txt"
            os.system(stringthing)
            print (stringthing)
            stringthing = "DONE"
    
        count += 1
    os.system("echo pigment{rgb 1}>>OUTPUT.txt")
    
    os.system("echo }>>OUTPUT.txt")


#os.system("pause")





