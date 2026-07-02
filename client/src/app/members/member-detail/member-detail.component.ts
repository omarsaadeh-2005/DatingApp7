import { Component, OnDestroy, OnInit, ViewChild, viewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Member } from 'src/app/_models/member';
import { GalleryItem, ImageItem } from '@ngx-gallery/core';
import { environment } from 'src/environments/environment';
import { TabDirective, TabsetComponent } from 'ngx-bootstrap/tabs';
import { MessageService } from 'src/app/_services/message.service';
import { Message } from 'src/app/_models/message';
import { PresenceService } from 'src/app/_services/presence.service';
import { AccountService } from 'src/app/_services/account.service';
import { User } from 'src/app/_models/user';
import { take } from 'rxjs';


@Component({
  selector: 'app-member-detail',
  templateUrl: './member-detail.component.html',
  styleUrls: ['./member-detail.component.css']
})

export class MemberDetailComponent implements OnInit, OnDestroy {
  @ViewChild('memberTabs',{static:true}) memberTabs?: TabsetComponent;
  member: Member= {} as Member;
  galleryImages: any[]=[];
  imageUrl = environment.imageUrl;
  activeTab?: TabDirective;
  messages: Message[]=[];
  user?: User;

  constructor(private accountService: AccountService, private route: ActivatedRoute, 
    private messageService: MessageService, public presenceService: PresenceService,
     private router:Router) {
        this.accountService.currentUser$.pipe(take(1)).subscribe({
          next: user =>{
            if(user) this.user=user;
          }
        });
        this.router.routeReuseStrategy.shouldReuseRoute = () => false;
     }

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

  ngOnDestroy(): void {
    this.messageService.stopHubConnection();
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
    if(this.activeTab.heading ==='Messages' && this.user){
        this.messageService.createHubConnection(this.user,this.member.userName);
    } else{
      this.messageService.stopHubConnection();
    }
  }

  
}
