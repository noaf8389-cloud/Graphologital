import numpy as np
import cv2
import json
from skimage import morphology
import traceback
import os
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3' # מסתיר הודעות מידע ואזהרות

import model_used_afect as model

# מחלקה של אובייקט מסוג תיבה תוחמת סביב תו
class Character:
    def __init__(self, x, y, w, h, char_img):
        self.x = x
        self.y = y
        self.w = w
        self.h = h
        self.img = char_img
        self.confidence = 0.0
        self.predicted_class = None
        self._skeleton = None
        self.split_depth = 0

class tis(Character):
    # פעולה בונה
    def __init__(self, img_path):
        self.img_path = img_path  # נתיב לתמונת הקלט

        config_path = os.path.join(os.path.dirname(__file__), "characters_config.json")
        with open(config_path, "r", encoding='utf-8') as f:
            self.config = json.load(f)

        raw_data = np.fromfile(self.img_path, dtype=np.uint8)
        self.img = cv2.imdecode(raw_data, cv2.IMREAD_COLOR)
        self.sorted_lines = []


    # ניקוי רקע התמונה וחידד הטקסט
    def __sharpen_text(self):
        # משתנים מהג'ייסון
        cfg = self.config["preprocessing"]
        kernel_size = tuple(cfg["kernel_size"])

        # אם התמונה צבעונית, המרה לשחור-לבן
        if len(self.img.shape) == 3:
            self.img = cv2.cvtColor(self.img, cv2.COLOR_BGR2GRAY)

        # שימוש בסף אדפטיבי והחלפת הפיקסלים השחורים והלבנים
        self.img = cv2.adaptiveThreshold(self.img, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C,cv2.THRESH_BINARY_INV, cfg["adaptive_block_size"], cfg["adaptive_c"])

        # הסרת רעש חיצוני קל
        kernel = cv2.getStructuringElement(cv2.MORPH_RECT, kernel_size)
        self.img = cv2.morphologyEx(self.img, cv2.MORPH_OPEN, kernel)

        # קריאה לפונציית סגירת סדקים באותיות
        self.__fill_minor_holes()


    # תיקון האותיות, סגירת סדקים
    def __fill_minor_holes(self):
        # משתנה מהג'ייסון
        max_hole_area = self.config["preprocessing"]["max_hole_area"]

        # מציאת הקונטרים עם הררכיה לזיהוי קונטרים פנימיים
        contours, hierarchy = cv2.findContours(self.img, cv2.RETR_CCOMP, cv2.CHAIN_APPROX_SIMPLE)
        if hierarchy is None:
            return

        # מעבר על הקונטרים, ומילוי הקונטורים הפנימיים והקטנים
        for i in range (len(contours)):
            if hierarchy[0][i][3] != -1:
                area = cv2.contourArea(contours[i])
                if area <= max_hole_area:
                    cv2.drawContours(self.img, contours, i, 255, -1)


    # זיהוי קונטרים בתמונה. (קונטור = קו מתאר שמייצג את גבולות האובייקט בתמונה)
    def __contours(self):
        contours, _ = cv2.findContours(self.img, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        return contours


    # מיון התיבות התוחמות
    def __dip_merge(self):
        bounding_boxes = []

        # זימון הפונקציה שמזהה את הקונטרים בתמונה
        contours = self.__contours()

        # מציאת התיבות החותמות עבור כל קנטור
        for contour in contours:
            area = cv2.contourArea(contour)
            if area > self.config["contours"]["min_contour_area"]:  # התעלמות מקבוצות קטנות מדי
                (x, y, w, h) = cv2.boundingRect(contour)  # x ו-y:הקואורדינטות של הפינה השמאלית העליונה של התיבה החותמת, w:רוחב התיבה, h:גובה התיבה
                new_box = Character(x, y, w, h, self.img[y: y + h, x: x + w])
                bounding_boxes.append(new_box)

        # מיון התיבות החותמות -  קודם לפי הרוחב - כלומר ציר לפי ציר האיקס, ואח"כ לחלק לשורות (בדומה למיון יציב)
        sorted_x = sorted(bounding_boxes, key=lambda k: k.x, reverse=True)  # מיון לפי האיקס
        line_bounding_boxes = []
        line_stats = [] # מערך מקביל למערך השורות לשמירת הסכום בכל שורה

        for x in sorted_x:  # חלוקה לשורות
            d = False  # דגל - האם נמצאה שורה מתאימה
            # מעבר על השורות האחרונות
            # חישוב טווח גובה השורה ההגיוני תוך נתינת משקל גבוה יותר לאותיות האחרונות
            for index, line in enumerate(line_bounding_boxes):

                # שליפת הנתונים הסטטיסטיים המוכנים של השורה הנוכחית
                stats = line_stats[index]
                line_top = stats['line_top']
                line_bottom = stats['line_bottom']
                line_height = stats['line_height']

                # חישוב מרכז האות הנוכחית
                char_center_y = x.y + x.h / 2

                # בדיקת התאמה לשורה כולל מרווח ביטחון של חצי מגובה השורה
                # ובדיקה אם האות האחרונה שהוכנסה לשורה ממוקמת בציר האיקס אחרי האות הזו - זוהי גלישת שורות
                if line_top - line_height / 2 < char_center_y < line_bottom + line_height / 2 and x.x < line[-1].x:
                    line.append(x)

                    # עדכון דינמי של הסטטיסטיקה של השורה
                    alpha = self.config["line_sorting"]["line_alpha"]
                    stats['line_top'] = (1 - alpha) * line_top + alpha * x.y
                    stats['line_bottom'] = (1 - alpha) * line_bottom + alpha * (x.y + x.h)
                    stats['line_height'] = stats['line_bottom'] - stats['line_top']
                    d = True
                    break

                # אם לא נמצאה שורה מתאימה - פתיחת שורה חדשה
            if not d:
                line_bounding_boxes.append([x])
                line_stats.append({'line_top': float(x.y), 'line_bottom': float(x.y + x.h), 'line_height': float(x.h)})

        self.sorted_lines = sorted(line_bounding_boxes, key=lambda k: k[0].y)  # מיון השורות המסודרות
        print("התיבות מויינו בהצלחה!")

    # קריאה למודל
    def __predict_character(self, character):
        model_input = cv2.bitwise_not(character.img)
        (character.predicted_class, character.confidence) = model.prepare_roi(model_input)

    # זיהוי התווים ואיחוד תווים מפוצלים
    def merge_bounding_boxes(self):
        cfg = self.config["merge"]
        # רשימה חדשה בשביל כל השורות המעודכנות
        new_sorted_lines = []

        for line in self.sorted_lines:
            new_list = []
            i = 0
            n = len(line)

            # שליחת כל תווי השורה למודל הזיהוי
            chars_to_predict = [c for c in line if c.predicted_class is None]
            if chars_to_predict:
                imgs = [cv2.bitwise_not(c.img) for c in chars_to_predict]
                results = model.predict_batch(imgs)
                for c, (cls, conf) in zip(chars_to_predict, results):
                    c.predicted_class = cls
                    c.confidence = conf

            while i < n:
                # התו הנוכחי
                c = line[i]

                # אם הגיע התו האחרון ברשימה
                if i == n-1:
                    new_list.append(c)
                    break

                c_next = line[i+1]

                # חפיפה בין שתי התיבות
                horizontal_gap = c.x - (c_next.x + c_next.w)

                # חפיפה בין שתי התיבות
                overlap_x = min(c.x + c.w, c_next.x + c_next.w) - max(c.x, c_next.x)

                # בדיקה אם האמצע של אחת התיבות נמצא בתוך רוחב התיבה השנייה
                contained_x = (c_next.x + c_next.w * 2/3 > c.x)

                # אם סיכוי לאיחוד נמוך - אין צורך לשלוח את האיחוד לזיהוי
                if not contained_x and horizontal_gap > (max(cfg["regular_gap_min"], min(c.w, c_next.w) * cfg["regular_gap_width_ratio"])) * cfg["skip_merge_gap_multiplier"]:
                    new_list.append(c)
                    i += 1
                    continue

                # חישוב תיבה תוחמת של 2 התיבות יחד
                y = min(c_next.y, c.y)
                h = max(c.y + c.h, c_next.y + c_next.h) - y
                x = c_next.x
                w = c.x + c.w - x
                c_new = Character(x, y, w, h, self.img[y: y + h, x: x + w])

                # שליחת התו המאוחד לזיהוי ע"י המודל
                self.__predict_character(c_new)

                # אם יש חפיפה משמעותית
                if contained_x and c_new.confidence > c.confidence * cfg["contained_confidence_ratio"]:
                    new_list.append(c_new)
                    i += 2
                    print("בוצע איחוד תיבות חופפות")
                    continue

                # איחוד התווים אם רמת הביטחון גבוהה והגודל הגיוני
                if c_new.w < c_new.h * cfg["regular_max_width_height_ratio"] and horizontal_gap < max(cfg["regular_gap_min"], min(c.w, c_next.w) * cfg["regular_gap_width_ratio"]) and c_new.confidence > c.confidence * cfg["regular_confidence_ratio"]:
                    # איחוד התווים
                    new_list.append(c_new)
                    i += 2
                    print("בוצע איחוד תיבות")

                else:
                    new_list.append(c)
                    i += 1

            # הוספת השורה המעודכנת למבנה המעודכן
            new_sorted_lines.append(new_list)

        # החלפת רשימת המחלקה ברשימה המעודכנת
        self.sorted_lines = new_sorted_lines


    # פיצול תווים מחוברים
    def split_connected_characters(self):
        cfg = self.config["split"]
        # רשימה חדשה בשביל כל השורות המעודכנות
        new_sorted_lines = []

        # מעבר על התווים לפי שורות
        for line in self.sorted_lines:
            new_list = []
            # הכנסת איברי הרשימה למחסנית כדי לאפשר פיצול חוזר יעיל
            stack = list(reversed(line))

            while stack:
                c = stack.pop()

                if c.split_depth >= self.config["split"].get("max_split_depth", 3):
                    new_list.append(c)
                    continue

                # תיבה קטנה או צרה מדי או צרה ביחס לגובה - לא מפצלים
                if c.w < cfg["min_width"] or c.h < cfg["min_height"] or c.w < c.h * cfg["min_width_height_ratio"]:
                    new_list.append(c)
                    continue

                # אם התו חשוד - יש לנסות לפצלו
                if c.w > c.h * cfg["suspect_width_height_ratio"] or float(c.confidence) < cfg["suspect_confidence"]:
                    check_cut = self.__find_cut_column(c)

                    if check_cut is None:
                        new_list.append(c)
                        continue

                    if 0 < check_cut < c.w:
                        # חלוקת התיבה ל-2 לפי חישוב ההיטל וזיהוי על ידי המודל
                        # התיבה הימנית
                        (x, y, w, h) = (c.x + check_cut, c.y, c.w - check_cut, c.h)
                        box_right = Character(x, y, w, h, self.img[y: y + h, x: x + w])

                        # התיבה השמאלית
                        (x, y, w, h) = (c.x, c.y, check_cut, c.h)
                        box_left = Character(x, y, w, h, self.img[y: y + h, x: x + w])

                        # שליחת 2 התווים לזיהוי
                        imgs = [cv2.bitwise_not(box_left.img), cv2.bitwise_not(box_left.img)]
                        results = model.predict_batch(imgs)
                        box_right.predicted_class, box_right.confidence = results[0]
                        box_left.predicted_class, box_left.confidence = results[1]

                        # עדכון מספר הפיצולים לכל תו
                        box_right.split_depth = c.split_depth + 1
                        box_left.split_depth = c.split_depth + 1

                        # פיצול התווים אם רמת הביטחון בתווים הנפרדים גדולה מזו של התו המאוחד
                        left_confidence = float(box_left.confidence)
                        right_confidence = float(box_right.confidence)
                        original_confidence = float(c.confidence)
                        avg_confidence = (left_confidence + right_confidence) / 2
                        min_confidence = min(left_confidence, right_confidence)
                        # אם אחוז הזיהוי לתו המקורי גבוה - פיצול רק אם מביא לתוצאות גבוהות במיוחד
                        if original_confidence > cfg["high_original_confidence"]:
                            should_split = (c.w > c.h * cfg["high_original_width_height_ratio"] and min_confidence >= cfg["high_min_confidence"] and avg_confidence >= original_confidence + cfg["high_avg_improvement"])

                        # אם אחוז הזיהוי לתו המקורי נמוך - פיצול אם משפר משמעותית את הזיהוי
                        else:
                            should_split = (min_confidence >= cfg["low_min_confidence"] and min_confidence > original_confidence * cfg["low_min_improvement_ratio"])

                        if should_split:
                            # הכנסת 2 התווים החדשים למחסנית כדי לאפשר פיצול חוזר
                            stack.append(box_left)
                            stack.append(box_right)
                            print("בוצע פיצול תווים")
                            continue

                new_list.append(c)

            # הוספת השורה המעודכנת למבנה המעודכן
            new_sorted_lines.append(new_list)

        # החלפת רשימת המחלקה ברשימה המעודכנת
        self.sorted_lines = new_sorted_lines


    # פונקציית עזר למציאת עמודת חיתוך
    # לפי: כמות הכתב בעמודה, עובי הכתב בעמודה, וקרבה לצומת השלד
    def __find_cut_column(self, c):
        cfg = self.config["cut"]

        # הגדרת טווח החיפוש ל60% המרכזיים של רוחב התיבה
        start_search = int(c.w * cfg["start_ratio"])
        end_search = int(c.w * cfg["end_ratio"])

        img = c.img == 255

        if not hasattr(c, "_skeleton") or c._skeleton is None:
            # יצירת שלד בעובי פיקסל אחד
            c._skeleton = morphology.skeletonize(img).astype(np.uint8)
        skeleton = c._skeleton

        # ---------------------------------------------
        # היטל אנכי
        # ההיטל המלא — מספר פיקסלי הכתב בכל עמודה בתיבה כולה
        full_hitel = np.sum(img, axis=0)

        hitel = full_hitel[start_search:end_search]
        if hitel.size == 0:
            return None

        # בחירת העמודות לפי ההיטל והשלד
        skeleton_cols = np.where(skeleton[:, start_search:end_search].any(axis=0))[0]

        if skeleton_cols.size == 0:
            return None

        # דירוג עמודות השלד לפי כמות הכתב בהן - פחות כתב = מועמד טוב יותר
        candidate_count = min(cfg["candidate_count"], skeleton_cols.size)
        candidate_indexes = skeleton_cols[np.argpartition(hitel[skeleton_cols], candidate_count - 1)[:candidate_count]]

        # ---------------------------------------------
        # חישוב עובי הכתב
        # מציאת מפת מרחקים לפקסלי הכתב לפי מרחק אוקלידי לפיקסל הרקע הקרוב - כלומר מתייחס גם לכיוונים אלכסוניים
        distance_map = cv2.distanceTransform(img.astype(np.uint8) * 255, cv2.DIST_L2, 3)

        # חישוב עומק הכתב הממוצע בכל עמודה
        foreground_count = img.sum(axis=0).astype(np.float32)
        thickness = np.divide(distance_map.sum(axis=0), foreground_count, out=np.full(c.w, np.inf, dtype=np.float32), where=foreground_count > 0)

        valid_thickness = thickness[np.isfinite(thickness)]
        if valid_thickness.size == 0:
            return None

        # העובי הטיפוסי של הכתב - מחושב באמצעות חציון
        typical_thickness = float(np.median(valid_thickness))

        # ---------------------------------------------
        # בדיקת כל העמודות החשודות
        best_cut = None
        # הציון האתחלתי הוא אינסוף
        best_score = float("inf")
        # קביעת גודל מינימלי לרכיב שלד
        total_skeleton_pixels = int(np.count_nonzero(skeleton))
        # קביעת גודל מינימלי לרכיב שלד משמעותי
        min_component_size = max(4, int(total_skeleton_pixels * cfg["min_component_ratio"]))

        # מעבר על העמודות החשודות
        for local_index in candidate_indexes:
            # המיקום האמיתי בתוך התיבה
            check_cut = int(local_index + start_search)

            # ---------------------------------------------
            # בדיקת גשר בשלד
            # הגדרת רצועה זמנית של 3 עמודות סביב החיתוך
            cut_start = max(0, check_cut - 1)
            cut_end = min(c.w, check_cut + 2)

            # מחיקת רצועות השלד סביב החיתוך ובדיקה האם זהו גשר - מחבר בין 2 חלקים נפרדים
            saved = skeleton[:, cut_start:cut_end].copy()
            skeleton[:, cut_start:cut_end] = 0
            component_count, _, stats, _ = cv2.connectedComponentsWithStats(skeleton, connectivity=8)
            skeleton[:, cut_start:cut_end] = saved

            # מספר רכיבי השלד המשמעותיים שנשארו
            significant_components = 0

            # מעבר על הרכיבים החל מאינדקס 1 כי אינדקס 0 הוא רקע
            for component_index in range(1, component_count):
                # מספר הפיקסלים ברכיב הנוכחי
                component_size = stats[component_index, cv2.CC_STAT_AREA]
                if component_size >= min_component_size:
                    significant_components += 1

            # אם מחיקת האזור לא יצרה לפחות שני חלקים, העמודה אינה גשר משמעותי
            if significant_components < 2:
                continue

            # ---------------------------------------------
            # בדיקת כמות הכתב בעמודה
            # מספר פיקסלי הכתב בהקודת החיתוך לפי ההינטל האנכי
            ink_at_cut = int (full_hitel[check_cut])

            # היחס בין גובה התיבה לפיקסלי הכתב
            ink_ratio = (ink_at_cut / max(c.h, 1))

            # אם אחוז גבוה מהעמודה מכיל כתב: כנראה שהחיתוך עובר בתוך קו משמעותי
            if ink_ratio > cfg["ink_ratio_max"]:
                continue

            # ---------------------------------------------
            # בדיקת עובי הכתב
            # השוואת עובי הכתב בעמודת החיתוך לעובי הטיפוסי של הכתב
            thickness_ratio = (thickness[check_cut] / max(typical_thickness, 1e-6))

            # אם אזור החיתוך רחב מהעובי הטיפוסי של הכתב
            if thickness_ratio > cfg["thickness_ratio_max"]:
                continue

            # ---------------------------------------------
            # בחירת החיתוך הטוב ביותר

            # ציון משולב - לפי עובי הכתב וכמות הכתב בעמודה
            score = (cfg["score_thickness_weight"] * thickness_ratio + cfg["score_ink_weight"] * ink_ratio)

            # אם המועמד הנוכחי טוב מהמועמד שנמצא עד כה
            if score < best_score:
                best_score = score
                best_cut = check_cut

        # עמודת החיתוך הטובה ביותר
        return best_cut


    # ציור מלבנים של תיבות חותמות
    def __draw_contours(self):
        display_image = cv2.bitwise_not(self.img)
        output_image = cv2.cvtColor(display_image, cv2.COLOR_GRAY2BGR)

        for line in self.sorted_lines:
            for box in line:
                # ציור התיבה החותמת על התמונה
                cv2.rectangle(output_image, (box.x, box.y), (box.x + box.w, box.y + box.h), (0, 255, 0),2)

        scale = self.config["display"]["scale"]
        cv2.imshow("boxes", cv2.resize(output_image, None, fx=scale, fy=scale))
        cv2.waitKey(0)
        cv2.destroyAllWindows()

    def draw_rectangles(self, output_path=None):
        self.__sharpen_text()  # ניקוי וחידוד את התמונה
        self.__dip_merge()  # מיון תיבות תוחמות
        # הצגה לפני איחוד ופיצול
        # self.__draw_contours()

        self.merge_bounding_boxes()  # זיהוי התווים ואיחוד תווים מפוצלים
        # הצגה אחרי איחוד
        # self.__draw_contours()

        self.split_connected_characters()  # פיצול תווים מאוחדים
        # הצגה אחרי פיצול
        # self.__draw_contours()

    def to_cs (self):
        # זימון הפונקציה שמזמנת את כל שלבי עיבוד התמונה והאלגוריתמיקה
        self.draw_rectangles()

        # שמירת התמונה המעובדת בקובץ חדש בתקיית המקור בשם processed_result.png
        processed_path = os.path.join(os.path.dirname(self.img_path), "processed_result.png")

        save_img = cv2.bitwise_not(self.img)

        # שמירה תומכת עברית (בדומה לקריאה):
        _, im_buf_arr = cv2.imencode(".png", save_img)
        im_buf_arr.tofile(processed_path)

        # בניית רשימת התווים לשליחה
        results = []
        for line in self.sorted_lines:
            line_data = []
            for c in line:
                # מחזירים רק את הנתונים הטקסטואליים/מספריים
                line_data.append({
                    'x': int(c.x),
                    'y': int(c.y),
                    'w': int(c.w),
                    'h': int(c.h),
                    'confidence': float(c.confidence),
                    'predicted_class': str(c.predicted_class)
                })
            if line_data:
                results.append(line_data)
        return results

# יודפס בצורת JISON רק אם הקובץ הורץ ישירות
if __name__ == "__main__":
    # הפעלת הפונקציה
    data = tis('.\w48_F_2_form46.JPG').to_cs()


    # הדפסת התגיות כדי ש-C# תדע איפה ה-JSON מתחיל ונגמר
    # זה מאפשר לך להמשיך להשתמש ב-print() רגילים ללוגים
    print("---DATA_START---")
    print(json.dumps(data))
    print("---DATA_END---")