import os
import io
from Division_into_characters import tis
from pytesseractRun import image_path

#טעינת הקובץ
# image_path = '.\w48_F_2_form46.JPG'
image_path = '.\w51_F_2_form2.JPG'

# #שם הקובץ
# file_name = os.path.basename(image_path)
#
# if file_name.endswith('.pdf'):
#     # המרת קובץ PDF לתמונה
#     img = convert_from_path(image_path)

# Creates a new image object.
# img = tis(image_path)
# if img is None:
#     print("Error: Image not found or unable to load.")
# type(img)
tis(image_path).draw_rectangles(output_path='result_rec.png')