fetch('data/current_book.json')
  .then(res => res.json())
  .then(book => {
    const tag = 'nekocode-22';
    const asin = book.asin;
    document.getElementById('book-cover').src =
      'https://m.media-amazon.com/images/P/' + asin + '.01._SL160_.jpg';
    document.getElementById('book-title').textContent = book.title;
    document.getElementById('book-author').textContent = book.author;
    document.getElementById('book-comment').textContent = book.comment;
    const url = 'https://www.amazon.co.jp/dp/' + asin + '/?tag=' + tag;
    document.getElementById('book-link').href = url;
    document.getElementById('book-btn').href = url;
  })
  .catch(() => {
    document.querySelector('.book-widget').style.display = 'none';
  });
