const API = '/api';

function getUser(){ try{return JSON.parse(localStorage.getItem('erenUser'));}catch{return null;} }
function setUser(u){ localStorage.setItem('erenUser', JSON.stringify(u)); }
function logout(){ localStorage.removeItem('erenUser'); window.location.href='index.html'; }
function roleLabel(role){ return role==='Client'?'Klient':role==='B2B'?'B2B User':role==='TravelAgent'?'Travel Agent':'Administrator'; }
function money(v){ return `${Number(v||0).toFixed(2)}€`; }
function fallbackImage(i){ const arr=['/images/package1.jpg','/images/package2.jpg','/images/package3.jpg','/images/package4.jpg','/images/package5.jpg','/images/package6.jpg','/images/package7.jpg','/images/package8.jpg','/images/festa1.png','/images/festa2.PNG','/images/festa3.PNG','/images/festa4.PNG']; return arr[i % arr.length]; }
async function jsonFetch(url, options){ const res=await fetch(url, options); let data={}; try{data=await res.json();}catch{} if(!res.ok) throw new Error(data.message||'Ndodhi një gabim.'); return data; }
function requireLogin(roles){ const u=getUser(); if(!u){ window.location.href='index.html?login=1'; return null; } if(roles && !roles.includes(u.role)){ window.location.href='index.html'; return null; } return u; }
function updateNavbar(){ const u=getUser(); const area=document.getElementById('authNav'); if(!area) return; if(!u){ area.innerHTML=`<a class="nav-link fw-bold" href="index.html?login=1">Login / Register</a>`; return; } const panel = u.role==='Administrator'?'admin.html':u.role==='TravelAgent'?'agent.html':'paketatTuristike.html'; area.innerHTML=`<a class="nav-link fw-bold" href="${panel}">${roleLabel(u.role)}</a><a class="nav-link" href="#" onclick="logout()">Dil</a>`; }

document.addEventListener('DOMContentLoaded', updateNavbar);
