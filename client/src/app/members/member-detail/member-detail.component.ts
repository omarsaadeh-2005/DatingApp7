import { Component, OnInit, ViewChild, viewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Member } from 'src/app/_models/member';
import { MembersService } from 'src/app/_services/members.service';
import { GalleryItem, ImageItem } from '@ngx-gallery/core';
import { environment } from 'src/environments/environment';
import { TabDirective, TabsetComponent } from 'ngx-bootstrap/tabs';
import { MessageService } from 'src/app/_services/message.service';
import { Message } from 'src/app/_models/message';


@Component({
  selector: 'app-member-detail',
  templateUrl: './member-detail.component.html',
  styleUrls: ['./member-detail.component.css']
})

export class MemberDetailComponent implements OnInit {
  @ViewChild('memberTabs',{static:true}) memberTabs?: TabsetComponent;
  member: Member= {} as Member;
  galleryImages: any[]=[];
  imageUrl = environment.imageUrl;
  activeTab?: TabDirective;
  messages: Message[]=[];

  constructor(private memberService: MembersService, private route: ActivatedRoute, 
    private messageService: MessageService) { }

  ngOnInit(): void {
    this.route.data.subscribe({
      next: data=> this.member=data['member']
    })

    this.route.queryParams.subscribe({
      next: params=>{
        params['tab'] && this.selectTab(params['tab'])
      }
    })
  }

  getImages(): GalleryItem[] {
   if (!this.member || !this.member.photos) return [];

  return this.member.photos.map(photo => {
    let src: string;
    if (photo.url.startsWith('https')) {
      src = photo.url;                          // full external URL
    } else if (photo.url.startsWith('/')) {
      src = 'https://localhost:5001' + photo.url; // /images/filename.png
    } else {
      src = 'https://localhost:5001/' + photo.url; // images/filename.png
    }
    return new ImageItem({ src, thumb: src });
  });
}

    selectTab(heading:string){
        if(this.memberTabs){
          this.memberTabs.tabs.find(x=>x.heading===heading)!.active=true
        }
    }

    loadMessages(){
    if(this.member){
      this.messageService.getMessageThread(this.member.userName).subscribe({
        next: messages => this.messages = messages
      })
    }
  }

  onTabActivated(data: TabDirective){
    this.activeTab=data;
    if(this.activeTab.heading==='Messages'){
        this.loadMessages();
    }
  }

  
}
