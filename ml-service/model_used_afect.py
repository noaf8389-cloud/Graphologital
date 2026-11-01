import cv2
import tensorflow as tf
import numpy as np
import os

IMG_SIZE   = (64, 64)
BATCH_SIZE = 64   # כמה אותיות לעבד במקביל

class_names = ['א','ב','ג','ד','ה','ו','ז','ח','ט','י','כ','ך','ל','מ','ם','נ','ן','ס','ע','פ','ף','צ','ץ','ק','ר','ש','ת']

# טעינת המודל פעם אחת
current_dir = os.path.dirname(os.path.abspath(__file__))
model_path  = os.path.join(current_dir, 'handwriting_model.keras')

try:
    model = tf.keras.models.load_model(model_path)
    # קומפילציה של הפונקציה לביצועים מהירים יותר
    _predict_fn = tf.function(lambda x: model(x, training=False), input_signature=[tf.TensorSpec(shape=[None, *IMG_SIZE, 1], dtype=tf.float32)])
    print("המודל נטען בהצלחה!")
except Exception as e:
    print(f"שגיאה בטעינת המודל: {e}")
    model = None
    _predict_fn = None


def _preprocess(img: np.ndarray) -> np.ndarray:
    # הכנת תמונה בודדת
    img = cv2.resize(img, IMG_SIZE, interpolation=cv2.INTER_AREA)
    return img.astype(np.float32) / 255.0

# זיהוי תמונה בודדת - עבור slip/ merge
def prepare_roi(car_img: np.ndarray):
    processed = _preprocess(car_img).reshape(1, *IMG_SIZE, 1)
    preds     = _predict_fn(processed).numpy()[0]
    idx       = int(np.argmax(preds))
    return idx, float(preds[idx]) * 100

# זיהוי רצף של תמונות
def predict_batch(images: list[np.ndarray]) -> list[tuple[int, float]]:
    if not images:
        return []

    # הכנת כל התמונות כמערך אחד
    arr = np.stack([_preprocess(img).reshape(*IMG_SIZE, 1) for img in images]).astype(np.float32)

    results = []
    # עיבוד ב-batches כדי לא לפוצץ את הזיכרון
    for start in range(0, len(arr), BATCH_SIZE):
        batch = arr[start : start + BATCH_SIZE]
        # קריאה אחת למודל
        preds = _predict_fn(batch).numpy()
        for pred in preds:
            idx = int(np.argmax(pred))
            results.append((class_names[idx], float(pred[idx]) * 100))

    return results