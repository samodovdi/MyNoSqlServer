class main{
    
    private static body : HTMLElement;
    
    private static requested  = false;
    
    private static renderTables(data:string[]):string{
        let result = "";
        
        for(let itm of data){
            result += '<span class="badge badge-info" style="margin-left: 5px">'+itm+'</span>';
        }
        
        
        return result;
        
    }
    
    private static renderHtml(data:IStatus[]):string{

        let html=`<table class="table table-striped"><tr><th>Id</th><th>Client</th><th>Ip</th><th>tables</th><th>Connected</th><th>Last Incoming</th></tr>`;
        
        for(let itm of data){
            
            html += '<tr><td>'+itm.id+'</td><td>'+itm.name+'</td><td>'+itm.ip+'</td><td>'+this.renderTables(itm.tables)+'</td>' +
                '<td>'+itm.connectedTime+'</td><td>'+itm.lastIncomingTime+'</td></tr>';
            
        }

        html += '</table>';

        return html;
    }
    
    static background(){
        
        if (!this.body)
            this.body = document.getElementsByTagName('body')[0];
        
        if (this.requested)
            return;
        
        this.requested = true;
        $.ajax({url:'/api/status', type:'get'})
            .then(result=>{
            this.requested = false;
            this.body.innerHTML = this.renderHtml(result);
        }).fail(()=>{
            this.requested = false;
        })
        

    }
}

let $:any;

window.setInterval(()=>main.background(), 1000);