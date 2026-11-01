import tensorflow as tf
import matplotlib.pyplot as plt
import os

import argparse

parser = argparse.ArgumentParser(description="Train the Hebrew letter CNN on the HHD dataset")
parser.add_argument("--train-dir", required=True, help="Path to HHD TRAIN folder")
parser.add_argument("--test-dir", required=True, help="Path to HHD TEST folder")
args = parser.parse_args()

train_dir = args.train_dir
test_dir = args.test_dir

# פרמטרים בסיסיים
IMG_SIZE = (64, 64) # בחירת גודל אחיד לכל התמונות
BATCH_SIZE = 32

# טעינת מאגר האימון
train_ds = tf.keras.utils.image_dataset_from_directory(
    train_dir,
    labels='inferred',
    label_mode='int',
    color_mode='grayscale', # אם התמונות בשחור-לבן, זה יחסוך זיכרון
    image_size=IMG_SIZE,
    batch_size=BATCH_SIZE # המודל לומד 32 תמונות בבת אחת וכך יותר יעיל
)

class_names = train_ds.class_names
num_classes = len(class_names)

# טעינת מאגר הבדיקה
test_ds = tf.keras.utils.image_dataset_from_directory(
    test_dir,
    labels='inferred',
    label_mode='int',
    color_mode='grayscale',
    image_size=IMG_SIZE,
    batch_size=BATCH_SIZE
)

# ממיר לתמונה בינארית
normalization_layer = tf.keras.layers.Rescaling(1./255)
train_ds = train_ds.map(lambda x, y: (normalization_layer(x), y))
test_ds = test_ds.map(lambda x, y: (normalization_layer(x), y))

# אופטימיזציה לביצועים
AUTOTUNE = tf.data.AUTOTUNE
train_ds = train_ds.cache().prefetch(buffer_size=AUTOTUNE)
test_ds = test_ds.cache().prefetch(buffer_size=AUTOTUNE)

# הגדרת אוגמנטציה
data_augmentation = tf.keras.Sequential([
  tf.keras.layers.RandomRotation(0.1), # סיבוב קל
  tf.keras.layers.RandomZoom(0.1),     # זום קל
])

# בניית המודל לפי שכבות נוירונים CNN
model = tf.keras.Sequential([
    # שימוש בשכבת Input מניעת אזהרות
    tf.keras.layers.Input(shape=(64, 64, 1)),
    data_augmentation,

    # שכבה ראשונה שמזהה מאפיינים בסיסיים
    tf.keras.layers.Conv2D(32, (3, 3), activation='relu', input_shape=(64, 64, 1)),
    tf.keras.layers.MaxPooling2D(2, 2),

    # שכבה שנייה למאפיינים מורכבים יותר
    tf.keras.layers.Conv2D(64, (3, 3), activation='relu'),
    tf.keras.layers.MaxPooling2D(2, 2),

    tf.keras.layers.Conv2D(128, (3, 3), activation='relu'),
    tf.keras.layers.MaxPooling2D(2, 2),

    # הפיכה למערך שטוח ומעבר לשכבות החלטה
    tf.keras.layers.Flatten(),
    tf.keras.layers.Dense(256, activation='relu'),

    # הוספת Dropout למניעת Overfitting
    tf.keras.layers.Dropout(0.4),

    # שכבת פלט - 24 נוירונים (אחד לכל אות)
    tf.keras.layers.Dense(num_classes, activation='softmax')
])

# הגדרת עצירה מוקדמת (מונע Overfitting)
early_stop = tf.keras.callbacks.EarlyStopping(
    monitor='val_loss',
    patience=5,
    restore_best_weights=True
)

model.compile(
    optimizer='adam',
    loss='sparse_categorical_crossentropy',
    metrics=['accuracy']
    )

if __name__ == "__main__":
    #הרצת האימון
    print("Starting training...")
    history = model.fit(train_ds, validation_data=test_ds, epochs=50, callbacks=[early_stop])

    #שמירת המודל
    model.save('handwriting_model.keras')
    print("Model saved successfully!")

    #הצגת התוצאות בגרפים
    # 9. הצגת גרפים (כדי להבין את התוצאות)
    acc = history.history['accuracy']
    val_acc = history.history['val_accuracy']
    epochs_range = range(len(acc))

    plt.plot(epochs_range, acc, label='Training Accuracy')
    plt.plot(epochs_range, val_acc, label='Validation Accuracy')
    plt.title('Training and Validation Accuracy')
    plt.legend()
    plt.show()




# מודל רשת קונבולוציה ב-TensorFlow - קוד מועתק מהמכלול
# def build_classifier():
#  model = Sequential(name="classifier")
#
#  model.add(Conv2D(64, kernel_size=5, strides=2, input_shape=img_shape, padding='same'))
#  model.add(LeakyReLU(alpha=0.2))
#  model.add(Dropout(0.3))
#
#  model.add(Conv2D(128, kernel_size=5, strides=2, padding='same'))
#  model.add(LeakyReLU(alpha=0.2))
#  model.add(Dropout(0.3))
#
#  model.add(Conv2D(256, kernel_size=5, strides=2, padding='same'))
#  model.add(LeakyReLU(alpha=0.2))
#  model.add(Dropout(0.3))
#
#  model.add(Conv2D(512, kernel_size=5, strides=2, padding='same'))
#  model.add(LeakyReLU(alpha=0.2))
#  model.add(Dropout(0.3))
#
#  model.add(Flatten())
#  model.add(Dense(1, activation='sigmoid'))
#
#  model.summary()
#  image = Input(shape=img_shape)
#  validity = model(image)
#  return Model(image, validity)