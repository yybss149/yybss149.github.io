(function(){
'use strict';
const data=window.damageAtlas;let game='gi';
const byId=id=>document.getElementById(id);
function show(item){
 window.mountDamageCalculator(game,item.id);
 byId('plain-guide').innerHTML=window.renderBeginnerContext(game,item.id)+(item.id==='direct'?window.renderFirstLesson(game):item.id==='lunar-team'?window.renderPlainGuide(game,item.id):'<details class="calculation-reading"><summary>计算逻辑与示例</summary>'+window.renderPlainGuide(game,item.id)+'</details>');
 byId('plain-guide').querySelectorAll('[data-first-lesson]').forEach(b=>b.onclick=()=>window.loadFirstDamageLesson(b.dataset.firstLesson));
 byId('advanced-reference').open=false;
 byId('description').hidden=true;
 byId('category').textContent=item.category;byId('title').textContent=window.beginnerTopics[game+'.'+item.id][0];byId('description').textContent=item.description;byId('status').textContent=item.status||'通用公式';
 byId('formula').innerHTML=item.factors.map((f,i)=>`${i?'<b>'+(i===1?'=':'×')+'</b>':''}<span>${f}</span>`).join('');
 byId('formula-note').textContent=item.note;
 byId('details').innerHTML=item.details.map((d,i)=>`<details><summary><span class="index">${String(i+1).padStart(2,'0')}</span><strong>${d[0]}</strong><small>${d[1]}</small></summary><div class="detail-body">${d[2]}</div></details>`).join('');
 byId('notes').innerHTML=item.notes.length?'<h3>计算时容易漏掉的细节</h3><ul>'+item.notes.map(n=>'<li>'+n+'</li>').join('')+'</ul>':'';
 byId('refs').innerHTML=item.refs.map(id=>{const s=data.sources.find(s=>s.id===id);return `<a href="${s.url}" target="_blank" rel="noopener noreferrer">[${s.id}] ${s.short} ↗</a>`}).join('');
 document.querySelectorAll('#types button').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.id===item.id)));
}
function setGame(next){game=next;document.body.dataset.game=game;document.querySelectorAll('[data-game]').forEach(b=>{if(b.tagName==='BUTTON')b.setAttribute('aria-pressed',String(b.dataset.game===game))});const items=data.games[game];byId('types').innerHTML=items.map(x=>`<button type="button" data-id="${x.id}" aria-pressed="false">${window.beginnerTopics[game+'.'+x.id][0]}</button>`).join('');byId('types').querySelectorAll('button').forEach(b=>b.addEventListener('click',()=>show(items.find(x=>x.id===b.dataset.id))));if(items.length)show(items[0]);}
document.querySelectorAll('.games button').forEach(b=>b.addEventListener('click',()=>setGame(b.dataset.game)));
byId('source-list').innerHTML=data.sources.map(s=>`<div class="source"><a href="${s.url}" target="_blank" rel="noopener noreferrer">[${s.id}] ${s.title} ↗</a><span>${s.scope}</span></div>`).join('');
byId('coverage').innerHTML=data.coverage||'';setGame(game);
})();
