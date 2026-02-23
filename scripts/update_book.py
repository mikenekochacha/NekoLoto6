import anthropic
import json
import os
from datetime import date
from pathlib import Path

def load_book_history():
    history_path = Path("NekoLoto6/NekoLoto6.Client/wwwroot/data/book_history.json")
    if history_path.exists():
        with open(history_path, "r", encoding="utf-8") as f:
            return json.load(f)
    return []

def save_book_history(history, new_book):
    history.insert(0, new_book)
    history = history[:10]
    history_path = Path("NekoLoto6/NekoLoto6.Client/wwwroot/data/book_history.json")
    with open(history_path, "w", encoding="utf-8") as f:
        json.dump(history, f, ensure_ascii=False, indent=2)
    return history

def update_current_book():
    client = anthropic.Anthropic(api_key=os.environ["ANTHROPIC_API_KEY"])

    history = load_book_history()
    past_titles = [b["title"] for b in history]
    past_titles_str = "、".join(past_titles) if past_titles else "なし"

    prompt = f"""あなたは統計・確率・データ分析・数学的思考に関する本を紹介する、
中年女性の書店員です。口調は柔らかく、親しみやすく、押しつけがましくありません。

ロト6の統計分析サイト「NekoLoto6」のサイドバーに表示する「今週の一冊」を選んでください。

## 選書の条件
- 統計学、確率論、データ分析、数学的思考、意思決定、行動経済学などに関連する本
- 一般向けで読みやすいもの（専門書すぎない）
- ラノベ・自己啓発系・典型的な宝くじ必勝法の本は不可
- 実際にAmazon.co.jpで販売されている本（ASINが実在するもの）
- 過去に紹介した本は除く: {past_titles_str}
- ASINは必ず実在するものを使用してください。不確かな場合は有名な定番書籍を選んでください。

## 出力形式
以下のJSON形式のみで返答してください。他の文字は一切含めないこと。

{{
  "title": "本のタイトル",
  "author": "著者名",
  "asin": "ASINコード（10桁英数字）",
  "comment": "書店員としての紹介文（200〜250文字）。吹き出しPOP風。柔らかい口調で。"
}}"""

    message = client.messages.create(
        model="claude-opus-4-6",
        max_tokens=1024,
        messages=[{"role": "user", "content": prompt}]
    )

    response_text = message.content[0].text.strip()
    # マークダウンコードブロックを除去
    if response_text.startswith("```"):
        lines = response_text.split("\n")
        lines = [l for l in lines if not l.startswith("```")]
        response_text = "\n".join(lines).strip()
    new_book = json.loads(response_text)
    new_book["date"] = date.today().isoformat()

    current_path = Path("NekoLoto6/NekoLoto6.Client/wwwroot/data/current_book.json")
    with open(current_path, "w", encoding="utf-8") as f:
        json.dump(new_book, f, ensure_ascii=False, indent=2)

    save_book_history(history, new_book)
    print(f"更新完了: {new_book['title']} / {new_book['author']}")

if __name__ == "__main__":
    update_current_book()
