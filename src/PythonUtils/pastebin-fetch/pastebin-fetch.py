import sys
import os
import re
import requests
import xmltodict
from argparse import ArgumentParser

# see: https://docs.python-guide.org/scenarios/xml/
# import untangle

# Hello
print("pastebin-fetch v0.1. (c) by Michael Hoffmeister, 2023")

# arguments

desc = """Fetching files from PasteBin.
"""

parser = ArgumentParser(\
    prog="pastebin-fetch.py",\
    usage="python3 pastebin-fetch.py {title of paste}",\
    description=desc\
)

parser.add_argument("title")

parser.add_argument("-d", "--devkey", action="store")
parser.add_argument("-u", "--user", action="store")
parser.add_argument("-p", "--passwd", action="store")
parser.add_argument("-o", "--outfile", action="store")

args = parser.parse_args()

# get api user key

title = args.title
apiDevKey = args.devkey
apiUsername = args.user
apiPassword = args.passwd

outFn = args.outfile
if outFn is None:
    outFn = "paste.txt"

if apiDevKey is None or apiUsername is None or apiPassword is None:
    print("Either --devkey, --user, --passwd or all are not given. Aborting!")
    exit(-1)

print("Sending API request to get api user key from dev-key, username and password..")

url = "https://pastebin.com/api/api_login.php"
postParams = {
    "api_dev_key" : apiDevKey,
    "api_user_name" : apiUsername,
    "api_user_password" : apiPassword
}
resp = requests.post(url, data = postParams)

apiUserKey = resp.text    

print("Api user key: " + apiUserKey)

# get lists of pastes

print("Sending API request for queries ..")

url = "https://pastebin.com/api/api_post.php"
postParams = {
    "api_dev_key" : apiDevKey,
    "api_user_key" : apiUserKey,
    "api_option" : "list",
    "api_results_limit" : "100"
}
resp = requests.post(url, data = postParams)
xml = "<pastes>" + resp.text + "</pastes>"
# xml = "<z><a><b>b1</b><b>b2</b></a><a><b>b1</b><b>b2</b></a></z>"

# parse to dict
xmldic = xmltodict.parse(xml)

# find title
foundPasteKey = None
for paste in xmldic['pastes']['paste']:
    if "paste_key" in paste and "paste_title" in paste:
        if paste['paste_title'] == "GCODE":
            foundPasteKey = paste['paste_key']
            break

# check if found
if foundPasteKey is None:
    print("Paste with key GCODE could not be found. Aborting!")
    exit(-1)

# build raw url
rawurl = 'https://pastebin.com/raw/' + foundPasteKey
print ("use raw URL: " + rawurl + " ..")

# get raw url
resp = requests.get(rawurl)
if not resp.ok:
    print("raw URL not retrieved properly. Aborting!")
    exit(-1)

# get content
# content = "%%FN=test123.nc%%" + resp.text
content = resp.text
print("Content has length: ", len(content))

# check for filename

match = re.search(r"%%FN=([^%]{1,256})%%", content)
if match:
    whole = match.group(0)
    outFn = match.group(1)
    content = content[len(whole):]

# write the content
print("Writing content to: " + outFn)
with open(outFn, 'w') as f:
    f.write(content)    

# ok
print("Done.")