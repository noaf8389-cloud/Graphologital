# https://github.com/Lotemn102/Text-Segmentation-From-Images/blob/master/tis.py

import numpy as np
import cv2
import os


'''
מחלקה tis:
'''
class tis:
    '''
    פעולה בונה:
    פרמטרים:
    - img_path (מחרוזת): נתיב לתמונה מבוססת טקסט לעבודה עליה.
    מחזירה:
    אובייקט text_segmentation חדש.
    '''
    def __init__(self, img_path):  #img_path(מחרוזת): נתיב לתמונה מבוססת טקסט
        self.img_path = img_path
        self.img = cv2.imread(self.img_path)

    '''
    פונקציה crop_text:
    פרמטרים:
    - output_dir (מחרוזת): נתיב לשמירת התמונה. אם אין ערך כפרמטר, התמונות יישמרו בספרייה בשם "text_segmentation" בתוך ספריית האב של התמונה המקורית.
    - איטרציות (int): מספר איטרציות ההרחבה שיבוצעו על התמונה. ערך ברירת המחדל הוא 5.
    - dilate (bool): האם להרחיב את הטקסט בתמונה או לא.
    ברירת המחדל היא 'True'. מומלץ להרחיב את התמונה לצורך פילוח טוב יותר.
    מחזירה: אין.
    שומר תמונות של כל המילים מהטקסט בנתיב הפלט.
    '''
    def crop_text(self, output_dir, iterations=5, dilate=True):
        sharp_img = self.__sharpen_text(self.img) #זימון פונציה שמנקה ומחדדת את התמונה
        conts = self.__contours(sharp_img, iterations, dilate) #זימון פונקציה שמוצאת קווי מתאר של מילים בטקסט

        if not output_dir:
            print("Please insert output directory.")
            return

        self.__crop_words(sharp_img, conts, output_dir) #זימון פונקציה שחותכת את המילים שפונקציית קווי המתאר מצאה

    '''
    הפונקציה draw_rectangles:

    פרמטרים:
    - output_path (מחרוזת): נתיב לשמירת התמונה. אם אין נתיב, ניתן כ-
    פרמטר, התמונה תישמר בקובץ האב המקורי של התמונה
    מַדרִיך.
    - איטרציות (int): מספר איטרציות ההרחבה שיתבצעו ב
    התמונה. ערך ברירת המחדל מוגדר ל-5.
    - להרחיב (bool): האם להרחיב את הטקסט בתמונה או לא.
    ברירת המחדל היא 'אמת'. מומלץ להרחיב את התמונה עבור
    פילוח טוב יותר.

    החזרות:
    אַף לֹא אֶחָד.
    שומר את התמונה בנתיב הפלט.
    '''
    def draw_rectangles(self, output_path=None, iterations=5, dilate=True):
        sharp_img = self.__sharpen_text(self.img) #זימון פונציה שמנקה ומחדדת את התמונה
        conts = self.__contours(sharp_img, iterations) #זימון פונקציה שמחזירה רשימה של קווי מתאר של מילים בטקסט

        if not output_path:
            parent_path = os.path.dirname(self.img_path)
            output_path = parent_path + 'draw_rectangles_result.png'

        for i in conts:
            self.__draw_rects(sharp_img, i, str(output_path)) # עבור כל תו - ציור מלבנים סביב קווי המתאר שנמצאו

    # ניקוי רקע התמונה וחידד הטקסט
    def __sharpen_text(self, img):
        # אם התמונה היא צבעונית, המרה לשחור-לבן
        if len(img.shape) == 3:
            img = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        thresh = cv2.adaptiveThreshold(img,255,cv2.ADAPTIVE_THRESH_GAUSSIAN_C,cv2.THRESH_BINARY,11,2) #חידוד הטקסט באמצעות המרה לבינארי
        return thresh

    # מציאת קווי מתאר של מילים בטקסט
    def __contours(self, img):
        im = self.__dilate_img(img, iterations)  # זימון פונקציה התמונה לפילוח טוב יותר
        # זיהוי קבוצות מחוברות
        num_labels, labels, stats, centroids = cv2.connectedComponentsWithStats(binary_image)  # זיהוי קבוצות מחוברות
        # קווי מתאר מתוך התמונה הבינארית
        contours, hierarchy = cv2.findContours(im, cv2.RETR_TREE, cv2.CHAIN_APPROX_SIMPLE)
        print(contours)
        return contours #החזרת קווי המתאר

    # הרחבת התמונה לפילוח טוב יותר בזיהוי קווי מתאר
    def __dilate_img(self, img, iterations):
        # ניקוי כל הרעשים
        denoised = cv2.fastNlMeansDenoising(img, dst=None, h=10)
        # הפיכת הצבעים של התמונה
        imagem = cv2.bitwise_not(denoised)
        kernel = np.ones((3, 3), np.uint8)
        dilate = cv2.dilate(imagem, kernel, iterations=iterations)
        # הפיכת הצבעים שוב לצבע המקורי
        final = cv2.bitwise_not(dilate)

        return final

    # חיתוך המילים שפונקציית קווי המתאר מצאה
    def __crop_words(self, img, conts, output_dir):
        counter = 0

        for i in conts:
            x, y, w, h = cv2.boundingRect(i)
            cropped = img[y:y + h, x:x + w]
            width, height = cropped.shape[:2]

            # הימנעות מחיתוך קווי מתאר קטנים מאוד, למשל נקודות, פסיקים
            if width * height > 300:
                cv2.imwrite(str(output_dir) + str(counter) + ".png", cropped)

            counter = counter + 1

        return

    # # ציור מלבנים סביב פונקציית קווי המתאר שנמצאה
    # def __draw_rects(self, img, contour, path_save):
    #     (x, y, w, h) = cv2.boundingRect(contour)
    #     img = cv2.rectangle(img, (x, y), (x + w, y + h), (0, 255, 0), 3)
    #     cv2.imwrite(path_save, img)

    def __draw_rects(self, img, stats, path_save, num_labels, path_save):
       # הנחת שהתמונה המקורית היא 'img' והסטטיסטיקות של התווים הם 'stats' ו-'num_labels'
       for i in range(1, num_labels):  # מתחילים מ-1 כדי להימנע מה-background
           x, y, w, h, area = stats[i]
           print(x, y, w, h)
           # בדוק אם האזור קטן מדי
           if area < 50:
               continue  # אם האזור קטן מ-50, לא נצייר
           # ציור מלבן סביב התווים
           cv2.rectangle(img, (x, y), (x + w, y + h), (0, 255, 0), 2)  # ציור תיבות על התווים
       # שמירת התמונה עם התווים המסומנים
       cv2.imwrite(path_save, img)




# Creates a new image object.
img = tis('.\w51_F_2_form2.JPG')
if img is None:
    print("Error: Image not found or unable to load.")
type(img)

# # Crops words from text and saves them to 'cropped_words' directory.
#img.crop_text(output_dir='cropped_words', iterations=5)
# Draws rectangles around all words in the text, and saves the result to
# 'result_rec.png'.
img.draw_rectangles(output_path='result_rec.png', iterations=2)
print(img.img_path)

# cv2.imshow('Image with Rectangle', img)
# cv2.waitKey(0)
# cv2.destroyAllWindows()