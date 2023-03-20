import os
import sys
import fileinput


with open('readytoconvert.txt', 'r') as file :
    filedata = file.read()
    
#TRUE HEX FORMAT

filedata = filedata.replace('0xcccccc', '0')
filedata = filedata.replace('0xcbcbcb', '0')
filedata = filedata.replace('0xc0c0c000', '0')
filedata = filedata.replace('0xe7e7e7', '0')
filedata = filedata.replace('0xaaaaaa', '1')
filedata = filedata.replace('0x2a39ba', '2')
filedata = filedata.replace('0xd44051', '3')
filedata = filedata.replace('0x50a9d8', '4')
filedata = filedata.replace('0x460619', '5')
#6
filedata = filedata.replace('0xa31124', '7')
filedata = filedata.replace('0xa3117c', '8')
filedata = filedata.replace('0x289e2f', '9')
filedata = filedata.replace('0x192419', '10')
filedata = filedata.replace('0x213021', '11')
filedata = filedata.replace('0x324932', '12')
filedata = filedata.replace('0x415d41', '13')
filedata = filedata.replace('0x4f714f', '14')
filedata = filedata.replace('0xa4b299', '15')
filedata = filedata.replace('0xa4b4a4', '16')
filedata = filedata.replace('0xb1c1b1', '17')
filedata = filedata.replace('0xd2e2d0', '18')
filedata = filedata.replace('0xe1eadd', '19')
filedata = filedata.replace('0xffffff', '20')
#21
filedata = filedata.replace('0x3744d5', '22')
filedata = filedata.replace('0x2540d1', '23')
filedata = filedata.replace('0x2158c7', '24')
filedata = filedata.replace('0x3aa3ed', '25')
filedata = filedata.replace('0x24a6ff', '26')
filedata = filedata.replace('0x61d9ff', '27')
filedata = filedata.replace('0xa31124', '28')
filedata = filedata.replace('0xb51328', '29')
filedata = filedata.replace('0xda4150', '30')
filedata = filedata.replace('0xff827d', '31')
filedata = filedata.replace('0xff9a70', '32')
filedata = filedata.replace('0xaebc2b', '33')
filedata = filedata.replace('0xc9d932', '34')
filedata = filedata.replace('0x6c2161', '35')
filedata = filedata.replace('0x852978', '36')
filedata = filedata.replace('0x94568d', '37')
filedata = filedata.replace('0xa494b0', '38')
filedata = filedata.replace('0x3d4f2d', '39')
filedata = filedata.replace('0x232e1a', '40')
filedata = filedata.replace('0x5a4024', '41')
filedata = filedata.replace('0xffc0a6', '42')
filedata = filedata.replace('0x2540d1', '43')
filedata = filedata.replace('0xffebae', '44')
filedata = filedata.replace('0xd4e2d0', '45')
filedata = filedata.replace('0xffe2d0', '46')
#47
filedata = filedata.replace('0x10505e', '48')
filedata = filedata.replace('0x252dff', '49')
filedata = filedata.replace('0x252d95', '52')


filedata = filedata.replace('0x10101', '47')
filedata = filedata.replace('0xc15ad', '21')
filedata = filedata.replace('0x91080', '6')


#GAMMA ROUNDED FORMAT
#6 COLFOR
filedata = filedata.replace('0xcccccc', '0')
filedata = filedata.replace('0x666666', '1')
filedata = filedata.replace('0xa70e14', '3')
filedata = filedata.replace('0x1465af', '4')
filedata = filedata.replace('0x5d0104', '7')
filedata = filedata.replace('0x5d0133', '8')
filedata = filedata.replace('0x132a13', '14')
filedata = filedata.replace('0x5e7151', '15')
filedata = filedata.replace('0x5e745e', '16')
filedata = filedata.replace('0x708770', '17')
filedata = filedata.replace('0xa4c1a0', '18')
filedata = filedata.replace('0xc0d1b8', '19')
filedata = filedata.replace('0xffffff', '20')
filedata = filedata.replace('0x1eb0ff', '27')
filedata = filedata.replace('0x5d0104', '28')
filedata = filedata.replace('0x750105', '29')
filedata = filedata.replace('0xb20d14', '30')
filedata = filedata.replace('0xff3834', '31')
filedata = filedata.replace('0xff5229', '32')
filedata = filedata.replace('0x6b8006', '33')
filedata = filedata.replace('0x94b00c', '34')
filedata = filedata.replace('0x26031e', '35')
filedata = filedata.replace('0x3b052f', '36')
filedata = filedata.replace('0x4b1743', '37')
filedata = filedata.replace('0x5e4b6e', '38')
filedata = filedata.replace('0x1a0d04', '41')
filedata = filedata.replace('0xff8661', '42')
filedata = filedata.replace('0xffd36b', '44')
filedata = filedata.replace('0xa7c1a0', '45')
filedata = filedata.replace('0xffc1a0', '46')

#5 COLFOR
filedata = filedata.replace('0x50a7d', '2')
filedata = filedata.replace('0xf0002', '5')
filedata = filedata.replace('0x55707', '9')
filedata = filedata.replace('0x20402', '10')
filedata = filedata.replace('0x40704', '11')
filedata = filedata.replace('0x81008', '12')
filedata = filedata.replace('0xd1b0d', '13')
filedata = filedata.replace('0x90ea9', '22')
filedata = filedata.replace('0x40da2', '23')
filedata = filedata.replace('0x31891', '24')
filedata = filedata.replace('0xa5dd7', '25')
filedata = filedata.replace('0x461ff', '26')
filedata = filedata.replace('0xb1306', '39')
filedata = filedata.replace('0x40602', '40')
filedata = filedata.replace('0x40da2', '43')
filedata = filedata.replace('0x1141c', '48')
filedata = filedata.replace('0x406ff', '49')
filedata = filedata.replace('0x4064c', '52')
#probably best to stick the other normal hex colours outlyiers here that fit


#4 COLFOR
#there is none

#3 COLFOR 
filedata = filedata.replace('0x137', '6')
filedata = filedata.replace('0x137', '21')

#2 COLFOR

#1 COLFOR
filedata = filedata.replace('0x0', '47')



with open('readytoconvert.txt', 'w') as file:
    file.write(filedata)