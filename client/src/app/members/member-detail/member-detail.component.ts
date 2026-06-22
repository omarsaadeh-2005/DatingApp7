import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Member } from 'src/app/_models/member';
import { MembersService } from 'src/app/_services/members.service';
import { GalleryItem, ImageItem } from '@ngx-gallery/core';
import { environment } from 'src/environments/environment';


@Component({
  selector: 'app-member-detail',
  templateUrl: './member-detail.component.html',
  styleUrls: ['./member-detail.component.css']
})
export class MemberDetailComponent implements OnInit {
  member: Member | undefined;
  galleryImages: any[]=[];
  imageUrl = environment.imageUrl;

  constructor(private memberService: MembersService, private route: ActivatedRoute) { }

  ngOnInit(): void {
    this.loadMember();
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

  loadMember(){
    const username=this.route.snapshot.paramMap.get('username');
    if(!username) return;
    this.memberService.getMember(username).subscribe({
      next: member=> {
        this.member=member;
        this.galleryImages = this.getImages();
      }
    })
  }

  
}
