import time
import torch
from torch.backends import cudnn
from matplotlib import colors

from EfficientDetY.backbone import EfficientDetBackbone
import cv2
import numpy as np

from EfficientDetY.efficientdet.utils import BBoxTransform, ClipBoxes
from EfficientDetY.utils.utils import invert_affine, postprocess, STANDARD_COLORS, standard_to_bgr, get_index_label, plot_one_box,aspectaware_resize_padding

class Detector():
    def __init__(self,weight_path):

        compound_coef = 8
        force_input_size = None  # set None to use default size
        # replace this part with your project's anchor config
        anchor_ratios = [(1.0, 1.0), (1.4, 0.7), (0.7, 1.4)]
        anchor_scales = [2 ** 0, 2 ** (1.0 / 3.0), 2 ** (2.0 / 3.0)]

        self.threshold = 0.2
        self.iou_threshold = 0.2



        self.obj_list = ['person', 'bicycle', 'car', 'motorcycle', 'airplane', 'bus', 'train', 'truck', 'boat', 'traffic light',
            'fire hydrant', '', 'stop sign', 'parking meter', 'bench', 'bird', 'cat', 'dog', 'horse', 'sheep',
            'cow', 'elephant', 'bear', 'zebra', 'giraffe', '', 'backpack', 'umbrella', '', '', 'handbag', 'tie',
            'suitcase', 'frisbee', 'skis', 'snowboard', 'sports ball', 'kite', 'baseball bat', 'baseball glove',
            'skateboard', 'surfboard', 'tennis racket', 'bottle', '', 'wine glass', 'cup', 'fork', 'knife', 'spoon',
            'bowl', 'banana', 'apple', 'sandwich', 'orange', 'broccoli', 'carrot', 'hot dog', 'pizza', 'donut',
            'cake', 'chair', 'couch', 'potted plant', 'bed', '', 'dining table', '', '', 'toilet', '', 'tv',
            'laptop', 'mouse', 'remote', 'keyboard', 'cell phone', 'microwave', 'oven', 'toaster', 'sink',
            'refrigerator', '', 'book', 'clock', 'vase', 'scissors', 'teddy bear', 'hair drier',
            'toothbrush']

        self.color_list = standard_to_bgr(STANDARD_COLORS)
        # tf bilinear interpolation is different from any other's, just make do
        input_sizes = [512, 640, 768, 896, 1024, 1280, 1280, 1536, 1536]
        self.input_size = input_sizes[compound_coef] if force_input_size is None else force_input_size


        model = EfficientDetBackbone(compound_coef=compound_coef, num_classes=len(self.obj_list),
                             ratios=anchor_ratios, scales=anchor_scales)
        model.load_state_dict(torch.load(weight_path, map_location='cpu'))
        model.requires_grad_(False)
        model.eval()
        self.model = model.cuda()



    def predict(self,cv2_frame):
        ori_imgs, framed_imgs, framed_metas = self.preprocess(cv2_frame, max_size=self.input_size)
        x = torch.stack([torch.from_numpy(fi).cuda() for fi in framed_imgs], 0)
        x = x.to(torch.float32).permute(0, 3, 1, 2)

        with torch.no_grad():
            features, regression, classification, anchors =self. model(x)
            regressBoxes = BBoxTransform()
            clipBoxes = ClipBoxes()
            out = postprocess(x,
                      anchors, regression, classification,
                      regressBoxes, clipBoxes,
                      self.threshold, self.iou_threshold)

        out = invert_affine(framed_metas, out)
        predict_image,detection_result=self.display(out, ori_imgs)
        return predict_image,detection_result

    def preprocess(self,cv2_frame,max_size=512, mean=(0.406, 0.456, 0.485), std=(0.225, 0.224, 0.229)):

        ori_imgs = [cv2_frame]
        normalized_imgs = [(img / 255 - mean) / std for img in ori_imgs]
        imgs_meta = [aspectaware_resize_padding(img, max_size, max_size,
                                            means=None) for img in normalized_imgs]
        framed_imgs = [img_meta[0] for img_meta in imgs_meta]
        framed_metas = [img_meta[1:] for img_meta in imgs_meta]

        return ori_imgs, framed_imgs, framed_metas
    
    def display(self,preds, imgs):
        detection_result=[]


        for i in range(len(imgs)):
            if len(preds[i]['rois']) == 0:
                continue

            imgs[i] = imgs[i].copy()

            for j in range(len(preds[i]['rois'])):
                x1, y1, x2, y2 = preds[i]['rois'][j].astype(np.int)
                obj = self.obj_list[preds[i]['class_ids'][j]]
              
                cx,cy=(x1+x2)/2,(y1+y1)/2
                res={"class_id":obj,"x1":str(x1),"y1":str(y1),"x2":str(x2),"y2":str(y2)}
                detection_result.append(res)
                #if obj not in detection_result.keys():
                #    detection_result[obj]=[x1,y1,x2,y2]
                #else:
                #    detection_result[obj].append([x1,y1,x2,y2])
                score = float(preds[i]['scores'][j])
                plot_one_box(imgs[i], [x1, y1, x2, y2], label=obj,score=score,color=self.color_list[get_index_label(obj, self.obj_list)])

        return imgs[0],detection_result


