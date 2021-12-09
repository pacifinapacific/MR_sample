
import torch 
import torchvision
from PIL import Image
from EfficientDetY.detect import Detector
import numpy as np
import cv2
import json


class PosPredictor():
	def __init__(self):
		self.BBoxDetector=Detector("EfficientDetY/weights/efficientdet-d8.pth")
		#template=cv2.imread( './input/world_cup_template.png')
		#self.template=cv2.resize(template,(1280,720))
	

	def predict(self,frame):

		frame=np.asarray(frame)
		frame=cv2.cvtColor(frame, cv2.COLOR_RGB2BGR)		
		predict_img,detection_result=self.BBoxDetector.predict(frame)

		#cv2.imwrite("a.jpg",predict_img)

			
		return detection_result

