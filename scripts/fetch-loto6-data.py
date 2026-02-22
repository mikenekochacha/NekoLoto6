"""
ロト6抽選データ取得スクリプト
KYO's LOTO6からCSVをダウンロードし、既存フォーマットに変換する。
"""

import csv
import io
import os
import sys
import time
import urllib.request
import urllib.error

SOURCE_URL = "https://loto6.thekyo.jp/data/loto6.csv"
MAX_RETRIES = 3
RETRY_INTERVAL_SEC = 1800  # 30分


def download_csv(url: str) -> str:
    """CSVをダウンロードしてUTF-8文字列として返す"""
    print(f"[INFO] ダウンロード: {url}")
    req = urllib.request.Request(url, headers={"User-Agent": "NekoLoto6-Updater/1.0"})
    with urllib.request.urlopen(req, timeout=30) as resp:
        raw = resp.read()
    # Shift-JIS → UTF-8
    return raw.decode("shift_jis")


def parse_kyo_csv(text: str) -> list[list[str]]:
    """KYO's形式のCSVをパースして行リストを返す"""
    reader = csv.reader(io.StringIO(text))
    rows = []
    for i, row in enumerate(reader):
        if i == 0:
            continue  # ヘッダースキップ
        if not row or not row[0].strip():
            continue
        rows.append(row)
    return rows


def convert_to_local_format(rows: list[list[str]]) -> list[str]:
    """
    KYO's形式 → 既存LOTO6_ALL.csv形式に変換
    - 日付をゼロパディング (2000/10/5 → 2000/10/05)
    - 数字をゼロパディング (2 → 02)
    - 販売実績列を追加 (0)
    - ヘッダーを既存形式に統一
    """
    header = "No,抽選日,数字１,数字２,数字３,数字４,数字５,数字６,数字Ｂ,１等口数,２等口数,３等口数,４等口数,５等口数,１等金額,２等金額,３等金額,４等金額,５等金額,キャリーオーバー,販売実績"
    lines = [header]

    for row in rows:
        if len(row) < 20:
            continue
        # 回号
        draw_no = row[0].strip()
        # 日付をゼロパディング
        date_parts = row[1].strip().split("/")
        date_str = f"{int(date_parts[0]):04d}/{int(date_parts[1]):02d}/{int(date_parts[2]):02d}"
        # 数字をゼロパディング
        nums = [f"{int(row[j].strip()):02d}" for j in range(2, 9)]
        # 賞金情報はそのまま
        prizes = [row[j].strip() for j in range(9, 20)]
        # 販売実績は元データにないので0
        sales = "0"
        line = ",".join([draw_no, date_str] + nums + prizes + [sales])
        lines.append(line)

    return lines


def get_latest_draw_no(csv_path: str) -> int:
    """既存CSVの最新回号を取得"""
    if not os.path.exists(csv_path):
        return 0
    with open(csv_path, "r", encoding="utf-8") as f:
        lines = f.readlines()
    for line in reversed(lines):
        line = line.strip()
        if line and not line.startswith("No"):
            return int(line.split(",")[0])
    return 0


def main():
    output_path = sys.argv[1] if len(sys.argv) > 1 else "LOTO6_ALL.csv"
    github_output = os.environ.get("GITHUB_OUTPUT", "")

    current_draw = get_latest_draw_no(output_path)
    print(f"[INFO] 現在の最新回号: {current_draw}")

    for attempt in range(1, MAX_RETRIES + 1):
        print(f"[INFO] 取得試行 {attempt}/{MAX_RETRIES}")
        try:
            text = download_csv(SOURCE_URL)
        except (urllib.error.URLError, OSError) as e:
            print(f"[ERROR] ダウンロード失敗: {e}")
            if attempt < MAX_RETRIES:
                print(f"[INFO] {RETRY_INTERVAL_SEC}秒後にリトライ...")
                time.sleep(RETRY_INTERVAL_SEC)
                continue
            print("[ERROR] 全リトライ失敗。終了します。")
            set_output(github_output, "updated", "false")
            sys.exit(1)

        rows = parse_kyo_csv(text)
        if not rows:
            print("[ERROR] CSVの解析に失敗しました。")
            set_output(github_output, "updated", "false")
            sys.exit(1)

        new_draw = int(rows[-1][0].strip())
        print(f"[INFO] 取得データの最新回号: {new_draw}")

        if new_draw > current_draw:
            print(f"[INFO] 新しいデータあり！ ({current_draw} → {new_draw})")
            lines = convert_to_local_format(rows)
            with open(output_path, "w", encoding="utf-8", newline="\n") as f:
                f.write("\n".join(lines) + "\n")
            print(f"[INFO] {output_path} に {len(rows)} 件書き込み完了")
            set_output(github_output, "updated", "true")
            set_output(github_output, "latest_draw", str(new_draw))
            return

        print(f"[INFO] 更新なし (現在: {current_draw}, 取得: {new_draw})")
        if attempt < MAX_RETRIES:
            print(f"[INFO] {RETRY_INTERVAL_SEC}秒後にリトライ...")
            time.sleep(RETRY_INTERVAL_SEC)

    print("[INFO] 更新データなし。終了します。")
    set_output(github_output, "updated", "false")


def set_output(github_output: str, key: str, value: str):
    """GitHub Actions出力変数を設定"""
    print(f"[OUTPUT] {key}={value}")
    if github_output:
        with open(github_output, "a") as f:
            f.write(f"{key}={value}\n")


if __name__ == "__main__":
    main()
