import { Component, Input, OnInit, ViewChild } from '@angular/core';
import { Message } from 'src/app/_models/message';
import { MessageService } from 'src/app/_services/message.service';
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { environment } from 'src/environments/environment';
import { TimeagoModule } from 'ngx-timeago';
import { FormsModule, NgForm } from '@angular/forms';

@Component({
  selector: 'app-member-messages',
  standalone: true,
  imports: [BrowserAnimationsModule,TimeagoModule, FormsModule],
  templateUrl: './member-messages.component.html',
  styleUrl: './member-messages.component.css'
})
export class MemberMessagesComponent implements OnInit{
  @ViewChild('messageForm')messageForm?: NgForm
  @Input() username?: string;
  @Input() messages:Message[]=[];
  imageUrl = environment.imageUrl;
  messageContent='';
  


  constructor(private messageService: MessageService) { }

  ngOnInit(): void {
  }

  sendMessage(){
    if(!this.username) return;
    this.messageService.sendMessage(this.username, this.messageContent).subscribe({
      next: message=> {
        this.messages.push(message);
        this.messageForm?.reset();
      }
    })
  }


}
