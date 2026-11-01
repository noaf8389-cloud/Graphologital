import cv2
import pytesseract
pytesseract.pytesseract.tesseract_cmd = r'C:\Program Files\Tesseract-OCR\tesseract.exe'

# קריאה של התמונה
image_path = '.\English.png'
img = cv2.imread(image_path)
#בדיקה שהתמונה נקראה
if img is None:
    print("Error: Image not found or can't be opened.")

# המרת התמונה לאפור כדי לשפר את הדיוק
gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
_, binary = cv2.threshold(gray, 150, 255, cv2.THRESH_BINARY)
text = pytesseract.image_to_string(binary)

# שיפור איכות התמונה
blurred = cv2.GaussianBlur(gray, (5, 5), 0)
_, binary = cv2.threshold(blurred, 150, 255, cv2.THRESH_BINARY)

# הדפסת הטקסט
print(pytesseract.image_to_string(img, lang='eng'))

# שימוש ב-Tesseract לקבלת נתוני התווים
data = pytesseract.image_to_data(gray, output_type=pytesseract.Output.DICT)

# הצגת קווי המתאר של התווים
n_boxes = len(data['text'])
for i in range(n_boxes):
    if int(data['conf'][i]) > 1:  # רק תווים עם ביטחון גבוה
        (x, y, w, h) = (data['left'][i], data['top'][i], data['width'][i], data['height'][i])
        cv2.rectangle(img, (x, y), (x + w, y + h), (255, 0, 0), 1)
        print(x,y,w,h)

# הצגת התמונה עם קווי המתאר
cv2.imshow('Image with character outlines', img)
cv2.waitKey(0)
cv2.destroyAllWindows()