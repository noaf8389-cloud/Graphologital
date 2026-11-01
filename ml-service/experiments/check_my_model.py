import tensorflow as tf
import numpy as np
import matplotlib.pyplot as plt
from tensorflow.keras.preprocessing import image

# הגדרות בסיסיות
IMG_SIZE = (64, 64)
# רשימת שמות המחלקות
class_names = ['א', 'ב', 'ג', 'ד', 'ה', 'ו', 'ז', 'ח', 'ט', 'י', 'כ', 'ך', 'ל', 'מ', 'ם', 'נ', 'ן', 'ס', 'ע', 'פ', 'ף', 'צ', 'ץ', 'ק', 'ר', 'ש','ת']

# טעינת המודל
try:
    model = tf.keras.models.load_model('handwriting_model.keras')
    print("המודל נטען בהצלחה!")
except Exception as e:
    print(f"שגיאה בטעינת המודל: {e}")


def predict_single_image(img_path):
    # טעינה ועיבוד התמונה
    img = image.load_img(img_path, target_size=IMG_SIZE, color_mode='grayscale')
    img_array = image.img_to_array(img)
    img_array = tf.expand_dims(img_array, 0)  # הפיכה ל-Batch (ממד נוסף)
    img_array /= 255.0  # נורמליזציה (כמו באימון)

    # ביצוע התחזית
    predictions = model.predict(img_array)
    # score = tf.nn.softmax(predictions[0])

    predicted_class = np.argmax(predictions[0])
    confidence = 100 * np.max(predictions[0])

    # הצגת התוצאה
    plt.imshow(img, cmap='gray')
    plt.title(f"Predict: {class_names[predicted_class]} ({confidence:.2f}%)")
    plt.axis('off')
    plt.show()

    print(f"התמונה היא כנראה האות: {class_names[predicted_class]} ברמת ודאות של {confidence:.2f}%")