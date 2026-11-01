import os
import sys
import traceback
from fastapi import FastAPI
from pydantic import BaseModel


from Division_into_characters import tis

app = FastAPI()

# האובייקט שהשרת מצפה לקבל מC#:
class ImageRequest(BaseModel):
    image_path: str

@app.post("/analyze")
def analyze_handwriting(request: ImageRequest):
    # בדיקה שהקובץ קיים בנתיב
    if not os.path.exists(request.image_path):
        return {"status": "error", "message": f"File not found: {request.image_path}"}

    try:
        # יצירת אובייקט מסוג מחלקת tis
        processor = tis(request.image_path)
        # הרצת הפונקציה המרכזית של המחלקה
        data = processor.to_cs()
        # שליחת הנתונים בחזרה ל-C# בתצורת JSON
        return {"status": "success", "predictions": data}

    except Exception as e:
        print("\n--- שרת פייתון נכשל! פירוט השגיאה המלאה: ---")
        traceback.print_exc() 
        print("-------------------------------------------\n")
        return {"status": "error", "message": f"Processing failed: {str(e)}"}


if __name__ == "__main__":
    import uvicorn

    # הרצת השרת
    uvicorn.run("communication_server:app", host="127.0.0.1", port=8000, reload=False)