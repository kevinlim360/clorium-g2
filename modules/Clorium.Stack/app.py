"""Small native desktop test UI; camera capture is deliberately separate."""
from pathlib import Path
import queue
import threading
import tkinter as tk
from tkinter import ttk, filedialog, messagebox
from clorium_stack import stack, Cancelled


def main(smoke=False):
    root=tk.Tk()
    root.title('Clorium Stack — 초점 합성 테스트')
    if smoke: root.withdraw()
    root.geometry('720x460')
    root.minsize(600,400)
    files=[]
    cancel=threading.Event()
    events=queue.Queue()
    destination=tk.StringVar()
    status=tk.StringVar(value='같은 구도로 초점을 달리 촬영한 사진을 2장 이상 선택하세요.')
    align=tk.BooleanVar(value=True)
    busy=False
    frame=ttk.Frame(root,padding=18)
    frame.pack(fill='both',expand=True)
    ttk.Label(frame,text='Clorium Stack',font=('맑은 고딕',20,'bold')).pack(anchor='w')
    ttk.Label(frame,text='사진 정렬 → 초점 합성 · 원본 보존 · 중복 결과 자동 번호').pack(anchor='w',pady=(2,12))
    bar=ttk.Frame(frame);bar.pack(fill='x')
    def pick():
        selected=filedialog.askopenfilenames(parent=root,title='초점이 다른 사진 선택',
                    filetypes=[('사진','*.jpg *.jpeg *.png *.tif *.tiff')])
        if selected:
            files[:]=selected
            listing.delete(0,'end')
            for path in files: listing.insert('end',Path(path).name)
            destination.set(str(Path(files[0]).parent/'Clorium_Stack'))
    def choose_folder():
        path=filedialog.askdirectory(parent=root,title='합성 결과 저장 폴더')
        if path: destination.set(path)
    select=ttk.Button(bar,text='사진 선택…',command=pick);select.pack(side='left')
    folder=ttk.Button(bar,text='저장 폴더…',command=choose_folder);folder.pack(side='left',padx=8)
    checkbox=ttk.Checkbutton(bar,text='자동 정렬',variable=align);checkbox.pack(side='right')
    listing=tk.Listbox(frame,height=8);listing.pack(fill='both',expand=True,pady=10)
    ttk.Label(frame,textvariable=destination,wraplength=660).pack(anchor='w')
    progress=ttk.Progressbar(frame,maximum=100);progress.pack(fill='x',pady=10)
    ttk.Label(frame,textvariable=status,wraplength=660).pack(anchor='w')
    controls=ttk.Frame(frame);controls.pack(fill='x',pady=(12,0))
    def run():
        nonlocal busy
        if len(files)<2:
            messagebox.showwarning('사진 선택','사진을 2장 이상 선택하세요.',parent=root);return
        busy=True;cancel.clear();progress['value']=0
        for widget in (select,folder,start,checkbox):widget.configure(state='disabled')
        stop.configure(state='normal')
        inputs=list(files);output=Path(destination.get())/(Path(inputs[0]).stem+'_stack.png');do_align=align.get()
        def worker():
            try:
                result=stack(inputs,output,align=do_align,cancel=cancel,
                             progress=lambda p,m:events.put(('progress',(p,m))))
                events.put(('done',result))
            except Exception as exc:events.put(('error',str(exc)))
        threading.Thread(target=worker,daemon=True).start()
    def request_stop():
        cancel.set();status.set('중지 요청 중… 현재 연산이 끝나면 중지합니다.')
    start=ttk.Button(controls,text='초점 합성 시작',command=run);start.pack(side='left')
    stop=ttk.Button(controls,text='중지',command=request_stop,state='disabled');stop.pack(side='left',padx=8)
    def poll():
        nonlocal busy
        while not events.empty():
            kind,data=events.get_nowait()
            if kind=='progress':progress['value']=data[0]*100;status.set(data[1])
            else:
                busy=False
                for widget in (select,folder,start,checkbox):widget.configure(state='normal')
                stop.configure(state='disabled')
                if kind=='done':status.set(f"완료 · {data['seconds']}초 · {data['output']}")
                else:status.set(data)
        root.after(100,poll)
    def close():
        if busy:request_stop();root.after(200,close)
        else:root.destroy()
    root.protocol('WM_DELETE_WINDOW',close)
    root.after(100,poll)
    if smoke: root.after(150,root.destroy)
    root.mainloop()


if __name__=='__main__':
    import sys
    main('--smoke-test' in sys.argv)
