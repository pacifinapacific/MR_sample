from flask import Flask,request,jsonify
import numpy as np
from io import BytesIO
from PIL import Image
import json
from models import PosPredictor
import base64


app = Flask(__name__)


@app.route('/', methods=['POST' , 'GET'])
def hello_world():
    if request.method == "POST":
        data = request.form['image'].encode("ASCII")
        img = Image.open(BytesIO(base64.b64decode(data)))
        #img = Image.open("combined.png")
        img.save("combined.png")
        info_list=[]
        detection_result=Predictor.predict(img)
        for detection in detection_result:
            info_list.append(detection)
        
        result={"detections":info_list}
        print(result)




 






    return jsonify(result)


if __name__=="__main__":
    Predictor=PosPredictor()
    app.run(host='0.0.0.0',port=5000,debug=True)
    #app.run()
