const $ = id => document.getElementById(id);
let categories = [];
let editingId = null;

function escHtml(s){
  if(s == null) return '';
  return String(s)
    .replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;')
    .replaceAll('"','&quot;');
}
function fmtPrice(v){
  return Number(v).toLocaleString('ru-RU',{minimumFractionDigits:2,maximumFractionDigits:2});
}
function fmtDate(s, withTime){
  if(!s) return '';
  const d = new Date(s);
  if(isNaN(d.getTime())) return s;
  return withTime
    ? d.toLocaleString('ru-RU',{hour12:false})
    : d.toLocaleDateString('ru-RU');
}
function toast(msg, type){
  const t = document.createElement('div');
  t.className = 'toast ' + (type || '');
  t.textContent = msg;
  $('toasts').appendChild(t);
  setTimeout(()=> t.remove(), 3500);
}

async function api(method, path, body){
  const opts = { method, headers: {} };
  if(body){
    opts.headers['Content-Type'] = 'application/json';
    opts.body = JSON.stringify(body);
  }
  const r = await fetch(path, opts);
  let data = null;
  try { data = await r.json(); } catch(e) {}
  if(!r.ok){
    const msg = (data && data.error) ? data.error : ('HTTP ' + r.status);
    throw new Error(msg);
  }
  return data;
}

async function loadDemo(){
  let d;
  try { d = await api('GET','/api/demo'); }
  catch(e){
    toast('Не удалось получить демо: ' + e.message, 'err');
    return;
  }
  if(d.startedAt){
    $('startedAt').textContent = 'Сервер запущен: ' + fmtDate(d.startedAt, true);
  }
  // ODBC snapshot
  if(d.productsError){
    $('odbcBox').innerHTML = `<div class="error">Ошибка ODBC: ${escHtml(d.productsError)}</div>`;
  } else if(!d.products || d.products.length === 0){
    $('odbcBox').innerHTML = '<div class="empty">Нет данных.</div>';
  } else {
    let h = '<table><thead><tr><th>Id</th><th>Название</th><th>Категория</th>'
      + '<th class="num">Цена</th><th class="num">Остаток</th></tr></thead><tbody>';
    for(const p of d.products){
      h += `<tr><td>${p.id}</td><td>${escHtml(p.name)}</td><td>${escHtml(p.category)}</td>`
        +  `<td class="num">${fmtPrice(p.price)}</td><td class="num">${p.stock}</td></tr>`;
    }
    h += '</tbody></table>';
    $('odbcBox').innerHTML = h;
  }
  // OleDb orders
  if(d.ordersError){
    $('oleBox').innerHTML = `<div class="error">Ошибка OleDb: ${escHtml(d.ordersError)}</div>`;
  } else if(!d.orders || d.orders.length === 0){
    $('oleBox').innerHTML = '<div class="empty">Нет данных.</div>';
  } else {
    let h = '<table><thead><tr><th>#</th><th>Покупатель</th>'
      + '<th class="num">Сумма</th><th>Статус</th><th>Дата</th></tr></thead><tbody>';
    for(const o of d.orders){
      h += `<tr><td>${o.id}</td><td>${escHtml(o.customer)}</td>`
        +  `<td class="num">${fmtPrice(o.total)}</td><td>${escHtml(o.status)}</td>`
        +  `<td>${fmtDate(o.createdAt)}</td></tr>`;
    }
    h += '</tbody></table>';
    $('oleBox').innerHTML = h;
  }
  // LINQ stats
  if(d.linqError){
    $('linqStatsBox').innerHTML = `<div class="error">Ошибка LINQ: ${escHtml(d.linqError)}</div>`;
    $('linqRecentBox').innerHTML = '';
  } else if(!d.linq){
    $('linqStatsBox').innerHTML = '<div class="empty">Нет данных.</div>';
    $('linqRecentBox').innerHTML = '';
  } else {
    const stats = d.linq.stats || [];
    if(stats.length === 0){
      $('linqStatsBox').innerHTML = '<div class="empty">Нет данных.</div>';
    } else {
      let h = '<table><thead><tr><th>Имя</th><th>Email</th>'
        + '<th class="num">Заказов</th><th class="num">Потрачено</th></tr></thead><tbody>';
      for(const s of stats){
        h += `<tr><td>${escHtml(s.name)}</td><td>${escHtml(s.email)}</td>`
          +  `<td class="num">${s.ordersCount}</td><td class="num">${fmtPrice(s.totalSpent)}</td></tr>`;
      }
      h += '</tbody></table>';
      $('linqStatsBox').innerHTML = h;
    }
    const recent = d.linq.recent || [];
    if(recent.length === 0){
      $('linqRecentBox').innerHTML = '<div class="empty">Нет заказов.</div>';
    } else {
      let h = '<table><thead><tr><th>#</th><th>Покупатель</th>'
        + '<th class="num">Сумма</th><th>Статус</th><th>Дата</th></tr></thead><tbody>';
      for(const o of recent){
        h += `<tr><td>${o.id}</td><td>${escHtml(o.customer)}</td>`
          +  `<td class="num">${fmtPrice(o.total)}</td><td>${escHtml(o.status)}</td>`
          +  `<td>${fmtDate(o.createdAt, true)}</td></tr>`;
      }
      h += '</tbody></table>';
      $('linqRecentBox').innerHTML = h;
    }
  }
}

async function loadCategories(){
  categories = await api('GET','/api/categories');
  const sel = $('fCategory');
  sel.innerHTML = categories.map(c => `<option value="${c.id}">${escHtml(c.name)}</option>`).join('');
}

async function loadProducts(){
  let data;
  try { data = await api('GET','/api/products'); }
  catch(e){
    $('productsBox').innerHTML = `<div class="error">Не удалось загрузить: ${escHtml(e.message)}</div>`;
    return;
  }
  const inserted = new Set(data.insertedIds || []);
  const updated  = new Set(data.updatedIds  || []);
  if(!data.items || data.items.length === 0){
    $('productsBox').innerHTML = '<div class="empty">Товаров нет.</div>';
    return;
  }
  let html = '<table><thead><tr>'
    + '<th>Id</th><th>Название</th><th>Категория</th>'
    + '<th class="num">Цена</th><th class="num">Остаток</th><th>Действия</th>'
    + '</tr></thead><tbody>';
  for(const p of data.items){
    let cls = '';
    if(inserted.has(p.id)) cls = 'row-insert';
    else if(updated.has(p.id)) cls = 'row-update';
    html += `<tr${cls ? ' class="'+cls+'"' : ''}>`
      + `<td>${p.id}</td>`
      + `<td>${escHtml(p.name)}</td>`
      + `<td>${escHtml(p.category)}</td>`
      + `<td class="num">${fmtPrice(p.price)}</td>`
      + `<td class="num">${p.stock}</td>`
      + `<td><div class="actions">`
      +    `<button class="btn-ghost warn" type="button" data-edit="${p.id}">Изменить</button>`
      +    `<button class="btn-ghost danger" type="button" data-del="${p.id}">Удалить</button>`
      + `</div></td></tr>`;
  }
  html += '</tbody></table>';
  $('productsBox').innerHTML = html;
  $('productsBox').querySelectorAll('[data-edit]').forEach(b => {
    b.addEventListener('click', () => {
      const id = Number(b.getAttribute('data-edit'));
      const row = data.items.find(x => x.id === id);
      openEdit(row);
    });
  });
  $('productsBox').querySelectorAll('[data-del]').forEach(b => {
    b.addEventListener('click', () => doDelete(Number(b.getAttribute('data-del'))));
  });
}

function renderRow(r){
  if(!r) return '<span class="small">(пусто)</span>';
  return `<b>#${r.id}</b> ${escHtml(r.name)}<br>`
    + `<span class="small">${escHtml(r.category)}, цена ${fmtPrice(r.price)}, остаток ${r.stock}</span>`;
}

async function loadLog(){
  let data;
  try { data = await api('GET','/api/log'); }
  catch(e){
    $('logBox').innerHTML = `<div class="error">${escHtml(e.message)}</div>`;
    return;
  }
  const entries = data.entries || [];
  $('logSummary').textContent = entries.length === 0 ? ''
    : `всего: ${entries.length} (INSERT ${data.insertedIds.length}, UPDATE ${data.updatedIds.length}, DELETE ${data.deletedIds.length})`;
  if(entries.length === 0){
    $('logBox').innerHTML = '<div class="empty">Операций пока нет. Используйте кнопки выше.</div>';
    return;
  }
  let html = '<table><thead><tr>'
    + '<th>Время</th><th>Операция</th><th>Id</th>'
    + '<th>Что произошло</th><th>Было</th><th>Стало</th><th>Комментарий</th>'
    + '</tr></thead><tbody>';
  for(const e of entries){
    const t = new Date(e.time);
    const tt = t.toLocaleTimeString('ru-RU',{hour12:false});
    const opCls = e.action === 'insert' ? 'op-insert'
                : e.action === 'update' ? 'op-update'
                : 'op-delete';
    html += `<tr>`
      + `<td>${tt}</td>`
      + `<td><span class="op ${opCls}">${e.action.toUpperCase()}</span></td>`
      + `<td>${e.productId}</td>`
      + `<td>${escHtml(e.title)}</td>`
      + `<td>${renderRow(e.before)}</td>`
      + `<td>${renderRow(e.after)}</td>`
      + `<td>${escHtml(e.comment)}</td>`
      + `</tr>`;
  }
  html += '</tbody></table>';
  $('logBox').innerHTML = html;
}

function openAdd(){
  editingId = null;
  $('modalTitle').textContent = 'Добавить товар';
  $('fId').value = '';
  $('fName').value = '';
  $('fPrice').value = '';
  $('fStock').value = '0';
  $('fDescription').value = '';
  $('fAnimal').value = 'universal';
  if(categories.length > 0) $('fCategory').value = categories[0].id;
  $('rowDescription').style.display = '';
  $('rowAnimal').style.display = '';
  $('overlay').classList.add('show');
  setTimeout(() => $('fName').focus(), 50);
}

function openEdit(row){
  editingId = row.id;
  $('modalTitle').textContent = `Изменить товар #${row.id}`;
  $('fId').value = row.id;
  $('fName').value = row.name;
  $('fPrice').value = row.price;
  $('fStock').value = row.stock;
  $('fDescription').value = '';
  $('fAnimal').value = 'universal';
  const c = categories.find(x => x.name === row.category);
  if(c) $('fCategory').value = c.id;
  $('rowDescription').style.display = 'none';
  $('rowAnimal').style.display = 'none';
  $('overlay').classList.add('show');
  setTimeout(() => $('fName').focus(), 50);
}

function closeModal(){ $('overlay').classList.remove('show'); }

async function submitForm(){
  const name = $('fName').value.trim();
  const price = Number($('fPrice').value);
  const stock = parseInt($('fStock').value, 10);
  const categoryId = Number($('fCategory').value);
  if(!name){ toast('Название не может быть пустым','err'); return; }
  if(isNaN(price) || price < 0){ toast('Цена должна быть числом ≥ 0','err'); return; }
  if(isNaN(stock) || stock < 0){ toast('Остаток должен быть целым ≥ 0','err'); return; }

  try {
    if(editingId == null){
      const body = {
        categoryId, name, price, stock,
        description: $('fDescription').value,
        animalType: $('fAnimal').value
      };
      const r = await api('POST','/api/products', body);
      toast('Добавлено: #' + r.product.id + ' ' + r.product.name,'ok');
    } else {
      const body = { categoryId, name, price, stock };
      const r = await api('PUT','/api/products/' + editingId, body);
      if(r.noop) toast('Изменений не было','ok');
      else toast('Изменено: #' + editingId,'ok');
    }
    closeModal();
    await Promise.all([loadProducts(), loadLog()]);
  } catch(e){
    toast(e.message,'err');
  }
}

async function doDelete(id){
  if(!confirm('Удалить товар #' + id + '?')) return;
  try {
    await api('DELETE','/api/products/' + id);
    toast('Удалено: #' + id,'ok');
  } catch(e){
    toast(e.message,'err');
  }
  await Promise.all([loadProducts(), loadLog()]);
}

document.addEventListener('DOMContentLoaded', async () => {
  $('btnAdd').addEventListener('click', openAdd);
  $('btnCancel').addEventListener('click', closeModal);
  $('btnSave').addEventListener('click', submitForm);
  $('overlay').addEventListener('click', e => { if(e.target.id === 'overlay') closeModal(); });
  document.addEventListener('keydown', e => { if(e.key === 'Escape') closeModal(); });
  await Promise.all([loadDemo(), loadCategories(), loadProducts(), loadLog()]);
});
